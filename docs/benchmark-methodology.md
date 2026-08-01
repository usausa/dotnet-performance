# ベンチマーク実施ガイドライン

パターンの効果検証に使う BenchmarkDotNet の構成と、測定を無意味にする落とし穴の回避策。
[performance-patterns.md](performance-patterns.md) の「実測例」は本ガイドラインに沿った測定を前提とする。

## 基本構成

- **MemoryDiagnoser を常時有効化** — 速度とアロケーションは常にセットで判断する
- **DisassemblyDiagnoser(printSource, exportDiff)を有効化** — 生成コードとコードサイズを確認する。速度差が誤差レベルでも、コードサイズで優劣を判断できるケースがある(インライン化への影響はコードサイズに現れる)
- ベンチマークを介さず単発で生成コードを見たい場合は、環境変数 `DOTNET_JitDisasm="メソッド名"` を設定して実行すると JIT アセンブリが標準出力に出る(Release ビルド + `DOTNET_TieredCompilation=0` 併用で最終コードを直接確認)
- **既定は最新ランタイム(net10.0)単独で測定する**。複数ランタイム並走は「世代で効果が変わるか」自体を問う検証(境界チェック除去イディオム、uint キャスト小細工のように新世代で消える最適化)に限定して使う

```csharp
public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddExporter(MarkdownExporter.GitHub);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(
            maxDepth: 3, printSource: true, exportDiff: true)));
        AddColumn(StatisticColumn.Min, StatisticColumn.Max, StatisticColumn.P90);
    }
}

// クラス側: 既定は net10.0 のみ。世代検証の対象クラスに限り net8 等のジョブを追加する
[MediumRunJob(RuntimeMoniker.Net10_0)]
```

## 測定を無意味にする落とし穴

### 1. 最適化による測定対象の消滅

結果が「空ループの下限値」に張り付いている場合、そのバリアントは JIT に完全に除去されており、実コストの比較になっていない。戻り値を返す・`[MethodImpl(MethodImplOptions.NoInlining)]` を付ける・BenchmarkDotNet の Consumer を使うなどで消滅を防ぐ。逆に、除去されたという事実自体が「その抽象化はゼロコスト」という結論になる場合もあるため、どちらを測っているのか自覚的であること。

### 2. 文字列インターンによる比較の短絡

文字列リテラルをそのままキーに使うと、参照等価により `string.Equals` が中身を比較せず短絡し、比較コードの測定にならない。実運用では外部入力(非インターン文字列)が来るため、プローブ文字列は必ずコピーして生成し、非インターンであることを検証してから測る。

```csharp
var probe = new string(literal.AsSpan());          // 非インターンのコピーを作る
Debug.Assert(string.IsInterned(probe) is null || !ReferenceEquals(string.IsInterned(probe), probe));
```

### 3. バリアント間の等価性未検証

測定前に全バリアントが同じ結果を返すことを `Verify()` として実行する(`BenchmarkRunner.Run` の前に呼ぶ)。速いが間違っている実装を測っても意味がない。手動 ref 走査系は特にバグが混入しやすい(終端 ref の計算ミス、二重走査での ref 進め忘れ等)。実例として、誤った終端判定が「常に真」になっていたループは JIT が判定ごと除去し、境界チェックなしの異常に速い偽の結果を長期間信じさせていた(修正後に再測定したところ最速から中位に転落した)。

### 4. 最良ケースだけの測定

宣言順アクセスなど理想形状だけで測ると、実運用で劣化する実装を選んでしまう。アクセス形状(順方向 / 逆順 / 部分アクセス / ミス混在)をパラメータ化し、「平均が速い」ではなく「形状に対して安定」な実装を選ぶ。

### 5. 例外パスの混入・未分離

成功ケースと失敗ケースは `[Params]` で分離して測る。例外スロー 1 回のコストは数 μs 規模で、他の最適化差を完全に覆い隠す(失敗パスが例外の場合、周辺をどれだけ最適化しても無意味になる)。

### 6. マイクロベンチ結果の過信

プリミティブ単体で 30 倍の差があっても、実処理(I/O・描画・支配的な計算)に埋め込むと 1.1 倍程度に希釈された実測例がある。最終判断は実ワークロードに近い形状のベンチマークで行い、マイクロベンチは「どの実装を候補にするか」の選別に使う。

### 7. #if による TFM 依存メソッドの混在(複数ランタイム実行時)

新しいランタイムにしかない API のベンチマークを `#if NET9_0_OR_GREATER` 等で同一クラスに混在させると、ホスト(最新 TFM)が発見したメソッドを下位ランタイムの子ビルドが解決できず、**そのランタイムの全ケースが NA になる**。TFM 依存の比較はクラスごと `#if` で分離し、そのクラスには対応するランタイムのジョブだけを付ける。

### 8. 準備処理の混入

衝突キーの探索・データ生成などの準備は `[GlobalSetup]` で行い、測定対象から外す。`IterationSetup` は測定精度を落とすため、可能な限り GlobalSetup + 状態リセット不要の設計にする。

## 判断基準

- **速度・アロケーション・コードサイズの 3 軸**で評価する。1 軸だけの改善は採用理由として弱い
- Ratio のベースラインは「現状の素直な実装」にし、改善幅がそのまま読めるようにする
- 効果が世代で消えた最適化は、パターンとしては「不要になった」と記録する(反パターン表へ)
