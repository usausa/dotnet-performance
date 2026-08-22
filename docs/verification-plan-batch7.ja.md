# 🧪 検証プラン 批次⑦: ref / Unsafe 系の追加候補

**位置づけ:** 採否判定が終わるまでの作業文書。判定後は内容を [benchmark-methodology.ja.md の検証キュー](benchmark-methodology.ja.md) の批次⑦行と、収録された候補の README 本文へ移し、**本ファイルは削除する**。移送先が既に日英 2 言語で維持されているため、本ファイルは日本語のみで作成している。

**背景:** README のカタログを「Span / Memory / Buffer / Unsafe / struct ref の一般的テクニック」として棚卸ししたところ、以下が未収録だった。

- ref 安全性の言語機能そのもの(`scoped` / `[UnscopedRef]` は README に 0 件、ref フィールドは不採用記録 R-12 にしか登場しない)
- struct レイアウト自体(サイズ・フィールド順・`LayoutKind`)。MEM-02 は「struct 配列 + ref」、MEM-04 は「16 バイト超は `in`」だが、**サイズを削る**話がその上流にない
- false sharing / キャッシュラインパディング(CON 族に 0 件)
- `Memory<T>` 側のコスト(`.Span` の解決コスト、配列への無コピー橋渡し、`MemoryManager<T>`)
- 再解釈 API の後継 `Unsafe.BitCast` が早見表にしかなく、本文には旧来の `Unsafe.As` だけが出ている
- `MemoryMarshal.Cast` を「ゼロコスト」と推奨しているが、アラインメント無検査と長さ切り捨ての注意がない

本プランはこの 13 候補を測って採否を決める。**実装済みのベンチマークは `benchmarks/PerformancePatterns.Benchmarks/Lab/` に配置済みで、`Program.cs` にも登録済み。** 判定が出るまで README・rejected-patterns・benchmark-methodology には一切手を入れていない。

---

## 📋 候補一覧

| ID | 候補 | 検証の問い | ベンチマーククラス | 事前予想 | 採用時の行き先 |
|:---:|---|---|---|---|---|
| 7-1 | `scoped` / `[UnscopedRef]` | `scoped` は codegen に出るか。`[UnscopedRef]` の ref 返しアクセサは get/set ペアに勝つか | `ScopedRefBenchmark` | scoped は生成コード一致。ref 返しは実差 | STK-10 + STK-01 注記 |
| 7-2 | ref フィールドの**正の**用途 | R-12 が「代わりにやること」で名指しした「フィールド粒度の構造読み」で Span+index に勝つか | `RefFieldStructReadBenchmark` | 拮抗〜わずかに ref 有利 | STK-11(または R-12 の訂正) |
| 7-3 | `GetValueRefOrNullRef` + `IsNullRef` | 存在確認 + 読み / 更新を 1 プローブに畳む効果。値サイズ依存はあるか | `ValueRefLookupBenchmark` / `ValueRefLargeLookupBenchmark` | 更新経路で 0.6〜0.8 倍。32 バイト値では読み取りでも差 | COL-07(または COL-01 拡張) |
| 7-4 | struct レイアウト最適化 | 同じ 4 フィールドで 32 バイトと 24 バイトの差が走査・引数渡しに出るか。`LayoutKind.Auto` は自動で詰めるか | `StructLayoutBenchmark` | 逐次は小差、散在で実差。Auto は Packed と同サイズ | MEM-05 |
| 7-5 | false sharing / パディング | 隣接スロットへの並行書き込みの罰則。64 バイトで足りるか 128 必要か。Interlocked 下でも残るか | `FalseSharingBenchmark` | 隣接は数倍遅い。128 が 64 以上に効く可能性 | CON-03 |
| 7-6 | `Memory<T>.Span` のコスト | ループ外へのホイストで何倍変わるか。裏の実体(配列 / MemoryManager)で変わるか | `MemorySpanCostBenchmark` | 要素ごとは大差、チャンクごとでも実差 | BUF-08 |
| 7-7 | `Unsafe.BitCast` | `Unsafe.As<TFrom,TTo>(ref v)` に対して具象型・ジェネリック双方でコストが増えないか | `BitCastBenchmark` | 生成コード一致 | **差なしでも本文昇格**(安全側の既定として) |
| 7-8 | `MemoryMarshal.Cast` の実コストと落とし穴 | `ReadUnaligned` / `BinaryPrimitives` に対する優劣。切り捨て・拡大の境界挙動 | `SpanReinterpretBenchmark` | Cast が最速。切り捨ては Verify で確定済み | 早見表注記 + BIT-04 / R-09 の条件追記 |
| 7-9 | 固定幅組み込み関数(Shuffle) | `Vector<T>` で書けないレーン置換で、`Vector128.Shuffle` と生 `Ssse3.Shuffle` に差が出るか | `VectorShuffleBenchmark` | スカラーに大差。Shuffle と Ssse3 は同一 | VEC-02 |
| 7-10 | `MemoryManager<T>` / `NativeMemory` | アンマネージド領域を `Memory<T>` として公開したときの `.Span` コスト | `MemorySpanCostBenchmark`(7-6 に同居) | 配列裏より重い | BUF-08 |
| 7-11 | `AreSame` / `ByteOffset` / `Overlaps` | ref からの index 復元は index を持ち回るより速いか。別名検査のコスト | `RefIdentityBenchmark` | index 復元は負ける(R-02 と同じ結論) | 早見表 + R-02 補足 |
| 7-12 | `Unsafe.Unbox<T>` | 既存のボックスを再確保せず更新できるか | `UnboxInPlaceBenchmark` | 時間は僅差、**割り当て軸で決定的** | STK-05 拡張 |
| 7-13 | `MemoryMarshal.TryGetArray` | `byte[]` を要求する旧 API への無コピー橋渡しの効果と、失敗時の退避経路 | `MemoryInteropBenchmark` | `ToArray` のコピーが丸ごと消える | BUF-08 |

---

## 🚦 判定フロー

[benchmark-methodology.ja.md の検証キュー](benchmark-methodology.ja.md)の 5 ステップをそのまま適用する。

1. 測定する(下記「実行手順」)
2. **有効** → README 本文へ収録(実装例・実測付き)
3. **無効** → [rejected-patterns.ja.md](rejected-patterns.ja.md) へ「どの世代まで有効だったか」付きで記録
4. **条件付き** → 適用条件を明記して収録
5. **信頼区間が重なる** → DisassemblyDiagnoser の Code Size で二分。生成コードに差があれば「➖ 誤差」として記録(不採用にしない)、命令列が一致すれば「差なし」として不採用

**本批次固有の追加ルール:**

- **7-7 は「差なし」でも採用**する。判断軸が速度ではなく安全性(サイズ不一致がコンパイル時/実行時に検出される)であるため、生成コード一致は「乗り換えて損がない」の証明になる。この例外は判定時に明記すること
- **7-2 はどちらに転んでも文書修正が発生する。** 勝てば STK-11 として収録、負ければ **R-12 の「代わりにやること」からカーソル型の推奨を削除**する(現状の記述が未測定のまま推奨になっている)
- **7-5 は `Environment.ProcessorCount` を結果に併記**する。`Workers` が物理コア数を超えた行は比較対象にならない

---

## ▶️ 実行手順

`Program.Main` は BenchmarkSwitcher の前に全 `Verify()` を実行するため、どのフィルタで起動しても等価性検証は全件走る。

批次⑦をまとめて実行:

```bash
dotnet run -c Release --project benchmarks/PerformancePatterns.Benchmarks --framework net10.0 -- --filter "*ScopedRefBenchmark*" "*RefFieldStructReadBenchmark*" "*ValueRefLookupBenchmark*" "*ValueRefLargeLookupBenchmark*" "*StructLayoutBenchmark*" "*FalseSharingBenchmark*" "*MemorySpanCostBenchmark*" "*MemoryInteropBenchmark*" "*BitCastBenchmark*" "*SpanReinterpretBenchmark*" "*RefIdentityBenchmark*" "*UnboxInPlaceBenchmark*" "*VectorShuffleBenchmark*"
```

候補ごとに実行する場合:

```bash
dotnet run -c Release --project benchmarks/PerformancePatterns.Benchmarks --framework net10.0 -- --filter "*ScopedRefBenchmark*"
```

等価性検証だけを先に通したい場合(ベンチマークを実行せずに Verify の失敗だけ見る):

```bash
dotnet run -c Release --project benchmarks/PerformancePatterns.Benchmarks --framework net10.0 -- --filter "*NoSuchBenchmark*"
```

`BenchmarkConfig` が MemoryDiagnoser と DisassemblyDiagnoser(`printSource` / `exportDiff`)を常時有効にしているため、**速度・割り当て・コードサイズの 3 軸**は追加設定なしで揃う。信頼区間が重なった候補の確定確認には JitDisasm を使う:

```bash
DOTNET_TieredCompilation=0 DOTNET_JitDisasm="*ConvertWithBitCast*" ./bin/Release/net10.0/PerformancePatterns.Benchmarks.exe
```

測定結果は `benchmarks/results/LAB-<クラス名>.md` に記録する(採用が決まった時点で `benchmarks/results/<パターンID>-<名前>.md` へ改名し、README の「実装例」欄から参照する)。

---

## 🔬 候補ごとの詳細

### 7-1 `scoped` / `[UnscopedRef]` — `ScopedRefBenchmark`

**測定内容:**

- 問い A: `SumSpan(ReadOnlySpan<byte>)` vs `SumScopedSpan(scoped ReadOnlySpan<byte>)`、`StepRef(ref long)` vs `StepScopedRef(scoped ref long)`
- 問い B: `GetSetPair`(getter + setter の 2 回アクセス)vs `UnscopedRefAccessor`(`[UnscopedRef] ref long GetSlot(int)` を 1 回)。格納は `[InlineArray(8)]` の struct なので ref は本当に `this` の内部を指す

**採用条件:** 問い B で `UnscopedRefAccessor` が信頼区間非重複で速い → STK-10 として収録。

**不採用でも残す記述:** 問い A が生成コード一致でも、`scoped` は **STK-01 の注意に必ず追記**する。理由は性能ではなく、ref struct を書く際に「呼び出し側の制約を外す唯一の手段」であり、現状の STK-01 が「制約がある」で止まって回避手段を書いていないため。

**注意:** `[UnscopedRef]` を外すと `GetSlot` は CS8170 でコンパイルできない。この事実自体が収録内容の核なので、ベンチマークのコメントに残してある。

---

### 7-2 ref フィールドの正の用途 — `RefFieldStructReadBenchmark`

**測定内容:** `[byte tag][ushort length][payload]` の可変長レコード 512 件を 4 通りで読む。

| バリアント | 形 |
|---|---|
| `ParseInlineIndex`(基準) | 呼び出し側に index 演算を直書き |
| `ParseSpanReader` | span + position を持つカーソル型 |
| `ParseSliceReader` | 残りを再スライスするカーソル型 |
| `ParseRefFieldReader` | `ref current` + `ref end`(R-12 が否定した形) |

**なぜこの形状か:** R-12 は「全要素走査」で ref フィールドを否定した。それは索引形が境界チェック除去 + 自動ベクトル化される形状なので当然の結果。**幅の違うフィールドを順に読む形状ではループが数え上げにならない**ため、R-12 の結論をそのまま持ち越せない。

**採用条件:** `ParseRefFieldReader` が `ParseInlineIndex` に対して信頼区間非重複で速い → STK-11 として収録し、R-12 と相互リンクして「反復は不可・構造読みは可」の線を引く。

**不採用時にやること:** 勝てなければ **R-12 の「代わりにやること」から「カーソル型は『フィールド粒度の構造読み』専用として使う」の一文を削除**する。現状は未測定の推奨が不採用記録の中に残っている状態。

---

### 7-3 `GetValueRefOrNullRef` — `ValueRefLookupBenchmark` / `ValueRefLargeLookupBenchmark`

**測定内容:** 1024 件の `Dictionary<string, T>` に 256 回プローブ。`Probe` パラメータで全ヒット / 半分ミスを分離(methodology 落とし穴 4)。プローブキーは格納キーと**別インスタンス**として生成しており、参照等価による短絡が起きない(落とし穴 2)。

- 読み取り: `TryGetValue`(基準)vs `GetValueRefOrNullRef` + `IsNullRef`
- 更新: `TryGetValue` + インデクサ書き戻し(2 プローブ)vs `GetValueRefOrNullRef` で in-place(1 プローブ)
- 値サイズ: `long`(8 バイト)と `Stat32`(32 バイト)の 2 クラス

**`ContainsKey` + インデクサの形は測っていない。** 本リポジトリでは CA1854 がビルド時にこの形を拒否するため、測るまでもなく採用余地がない。読み取りの基準は `TryGetValue` とした。

**`GetValueRefOrAddDefault` は含めない。** COL-01 で測定済みであることに加え、半分ミス条件では挿入が走って辞書が反復ごとに成長し、測定が壊れるため。

**採用条件:** 更新経路で実差 → COL-07 として収録(または COL-01 を拡張)。読み取りのみが誤差でも、32 バイト値で差が出るなら「値サイズ条件付き」で収録する。

---

### 7-4 struct レイアウト — `StructLayoutBenchmark`

**測定内容:** 同じ 4 フィールド(`long`, `int`, `long`, `int`)を持つ 3 型を 16384 要素走査。

| 型 | 宣言 | サイズ |
|---|---|---|
| `LayoutPadded` | `Sequential`、長短交互の宣言順 | 32 バイト(8 バイトのパディング) |
| `LayoutPacked` | `Sequential`、広い順に並べ替え | 24 バイト |
| `LayoutAuto` | `Auto`、宣言順は Padded と同じ | ランタイム任せ |

サイズは `Verify()` で `Unsafe.SizeOf` を assert しており、前提が崩れたら実行時に落ちる。

走査は**逐次**と**散在**(固定擬似乱数の訪問順)の 2 形状。逐次はプリフェッチャが吸収するため、フットプリント差は散在側に出るはず。加えて `ByValuePadded` / `ByValuePacked` で呼び出し境界のコピー差も測る(MEM-04 の 32 バイト行と突き合わせる)。

**採用条件:** 散在走査で実差 → MEM-05 として収録。

**結論の中心になりうる点:** C# の struct は既定で `Sequential` として出力されるため、**宣言順を放置するとパディングが残る**。`LayoutAuto` が `LayoutPacked` と同サイズなら、対策は「明示的に `Auto` を付ける」か「自分で広い順に並べる」の二択になる。この使い分けが収録内容の核。

---

### 7-5 false sharing — `FalseSharingBenchmark`

**測定内容:** `Workers` 個のワーカーがそれぞれ自分のスロットへ 50,000 回書く。

- `AdjacentVolatile`(基準): `long[]` の隣接スロット
- `Padded64Volatile`: `[StructLayout(LayoutKind.Explicit, Size = 64)]`
- `Padded128Volatile`: 同 128 バイト(BCL の `PaddingHelpers` が 128 を使う理由 = 隣接ラインのプリフェッチ)
- `AdjacentInterlocked` / `Padded128Interlocked`: 同じレイアウトを interlocked 書き込みで

素の書き込みは JIT にレジスタへホイストされて消えうるため `Volatile.Write` / `Volatile.Read` を使っている(methodology 落とし穴 1)。ワーカーのデリゲートは `GlobalSetup` で 1 回だけ生成しており、反復ごとの割り当てがどの variant にも乗らない。

**採用条件:** 隣接 vs パディングが実差 → CON-03 として収録。

**注意:**

- 時間は `Workers` に比例するため、**比率は同一 `Workers` 内でのみ**意味がある
- `Workers` が物理コア数を超える行は比較対象外。結果には `Environment.ProcessorCount` を併記する
- ストライド付き `long[]`(stride 8)は `Padded64` と等価のはずなので測っていない。収録時は「配列で済ませる形」として記述に含める

---

### 7-6 / 7-10 / 7-13 `Memory<T>` — `MemorySpanCostBenchmark` / `MemoryInteropBenchmark`

**測定内容(7-6 / 7-10):**

- チャンク処理(16 バイト × 256): `.Span` を 1 回ホイスト → `span.Slice` vs `memory.Slice(...).Span` を毎回
- 要素ごと: ホイストした `span[i]` vs `arrayMemory.Span[i]`(極端形)
- 裏の実体: 配列裏 vs `NativeMemoryManager<byte>`(`NativeMemory.Alloc` を `MemoryManager<T>` で包んだもの)

**測定内容(7-13):** `byte[] + offset + count` しか受け取らない旧 API へ渡す 3 経路。

- `ToArrayCopy`(基準): `memory.ToArray()`
- `TryGetArraySegment`: `MemoryMarshal.TryGetArray` で `ArraySegment` を取り出す(コピーなし)
- `ManagerFallbackCopy`: 配列裏でない `Memory` に対して `TryGetArray` が**失敗**し、コピーへ退避する経路

`Verify()` は「MemoryManager 裏の `Memory` に対して `TryGetArray` が false を返す」ことも assert している。これが退避経路が必要な理由そのものなので、正しさの前提として固定してある。

**採用条件:** ホイストの有無で実差 → BUF-08 として収録。`.Span` のコストと `TryGetArray` の橋渡しを 1 パターンにまとめる。

**R-13 との関係:** アンマネージド領域は GC 対象外なので、R-13(POH 常駐バッファ)の「確保が 19.3 倍」という論点は当てはまらない。収録時は両者の適用範囲の違いを明記する。

---

### 7-7 `Unsafe.BitCast` — `BitCastBenchmark`

**測定内容:**

- 具象型: `Unsafe.As<float, int>(ref v)`(基準)vs `Unsafe.BitCast<float, int>(v)` vs `BitConverter.SingleToInt32Bits(v)`
- ジェネリック(JIT-03 の形): `ConvertWithAs<int>` vs `ConvertWithBitCast<int>`。`BitCast` のサイズ検査がインスタンス化ごとに畳まれるかを見る

**採用条件(特例):** 生成コードが一致した場合でも**採用**する。判断軸が速度ではなく、サイズ不一致が検出されること(`Unsafe.As` は黙って壊れる)。README では TYP-05 / JIT-03 / SEQ-02 の本文に「同サイズ値型の再解釈は `BitCast` を既定にする」を追記し、`Unsafe.As<TFrom,TTo>` はサイズが異なる再解釈に限る、と線を引く。

**もし遅かった場合:** ジェネリック側だけ遅いなら「具象型は BitCast、ジェネリックは As」の条件付きになる。その場合はコードサイズも併記する。

**net8 での制約:** `Unsafe.BitCast` は .NET 8 では `TTo : struct` 制約を要求するため、ジェネリックヘルパーには `where T : struct` を付けている(net10 では不要だが、比較対象の `ConvertWithAs` にも同じ制約を付けて条件を揃えてある)。

---

### 7-8 `MemoryMarshal.Cast` — `SpanReinterpretBenchmark`

**測定内容:** 4096 バイトを 1024 個の `int` として読む 3 経路。`MemoryMarshal.Cast`(基準)/ `Unsafe.ReadUnaligned` ループ / `BinaryPrimitives.ReadInt32LittleEndian` ループ。

**Verify で固定した挙動(こちらが本題):**

- 10 バイトを `Cast<byte, int>` すると長さ 2 になり、**末尾 2 バイトが黙って消える**
- `int[3]` を `Cast<int, byte>` すると長さ 12 になる(この掛け算方向に int オーバーフローのガードがある)

**行き先:** 速度で新パターンにはならない見込み。結果は次の 2 箇所の**注記**として使う。

- 早見表の `MemoryMarshal.Cast` 行に「アラインメント無検査(Arm で `DataMisalignedException`)・長さ変化」を追記
- BIT-04 と R-09 の「Cast はゼロコスト」という記述に、上記が成り立つ条件を添える

---

### 7-9 固定幅組み込み関数 — `VectorShuffleBenchmark`

**測定内容:** `uint` 1021 件のエンディアン反転(= 純粋なレーン置換)。

- `ScalarReverse`(基準): `BinaryPrimitives.ReverseEndianness` ループ
- `Vector128ShuffleReverse`: `Vector128.Shuffle`(移植可能。インデックスを正規化する)
- `Ssse3ShuffleReverse`: `Ssse3.Shuffle`(生の ISA 組み込み関数。正規化なし)
- `VectorArithmeticReverse`: `Vector<uint>` のシフトとマスクで同じ置換を組み立てる(シャッフルが無い幅非依存形)

要素数を 1021(ベクトル幅の倍数でない)にしてあるため、全バリアントで端数経路が走る。`Vector128.IsHardwareAccelerated` / `Ssse3.IsSupported` のガードとスカラーフォールバックも各バリアントに入っており、`Verify()` は 3 形とスカラーの出力全一致を確認する。

**採用条件:** シャッフル形がスカラーに対して実差 → VEC-02 として収録。`Vector128.Shuffle` と `Ssse3.Shuffle` が同一なら「**定数マスクなら移植可能な `Vector128.Shuffle` を既定にしてよい**(正規化は畳まれる)」が指針になる。

**VEC-01 との関係:** VEC-01 は「幅非依存の `Vector<T>` を既定にし、固定幅シャッフルが要るときだけ落とす」と書いているが、その「落とした先」の数値が無い。本候補はその欠けている半分を埋めるもの。

**スコープ外:** `Vector128.ShuffleNative`(.NET 9+)は測っていない。ベンチマークプロジェクトが net8.0 も対象にしているため、methodology の落とし穴 7(`#if` による TFM 依存メソッドの混在)を避けた。採用が決まってから別クラスで追加する。

---

### 7-11 ref の同一性 / 位置 — `RefIdentityBenchmark`

**測定内容:**

- index 復元: `for` で index を持ち回る(基準)vs `foreach (ref var item in span)` + `Unsafe.ByteOffset` で index を逆算
- 別名検査: `Unsafe.AreSame(先頭 ref 同士)` vs `MemoryExtensions.Overlaps`

**事前予想:** index 復元は R-02 と同じ理由で負ける(索引形の方が JIT に優しい)。負けること自体が「ref 中心に書くと index が高くつく」という設計上の情報になるので、結果がどちらでも記録する価値がある。

**行き先:** 新パターンにはならない見込み。早見表へ 3 API を追加し、R-02 の補足として「ref から index を戻すくらいなら index を持ち回る」を数値付きで記録する。

---

### 7-12 `Unsafe.Unbox<T>` — `UnboxInPlaceBenchmark`

**測定内容:** 256 個の boxed struct を更新する 2 経路。

- `UnboxCopyRebox`(基準): アンボックス → コピーを更新 → 再ボックス(**更新ごとに 1 アロケーション**)
- `UnsafeUnboxInPlace`: `ref var v = ref Unsafe.Unbox<BoxedCounter>(box)` で既存のボックス内部を直接更新

`Verify()` は合計値の一致に加えて、**ボックスのインスタンスが差し替わっていないこと**(`ReferenceEquals`)も確認する。同一性が保たれることが再ボックス形との本質的な差だから。

**採用条件:** 割り当てが 0 になれば採用(時間軸が誤差でも可)。行き先は STK-05(ボックス化回避)の拡張。STK-05 は「ボックス化させない」話なので、「既に存在するボックスを扱う」場合の追記になる。

**注意として書くこと:** 型が一致しないと `InvalidCastException` になる(`Unsafe.As` と違って型チェックはある)が、boxed でない参照を渡した場合の挙動は未定義。適用は「自分が箱に入れたもの」に限る。

---

## 🧹 併せて直す既存の不整合

本批次の測定とは独立に、棚卸しで見つかった **README 内の自己矛盾** がある。批次⑦の結果を書き込む前に直しておくと、新しい記述と整合が取れる。

| 対象 | 現状 | 直し方 |
|---|---|---|
| MEM-02 効果欄 | 「走査中心の処理でも**約 1.5 倍**」と書いてあるが、同じ節の実測結果は「struct 412.9 ns ≒ struct コピー 414.7 ns ≒ class 配列 401.6 ns(**全て約 3% 以内**)」 | 実測欄が既に「採用理由は構造面であってマイクロ計測の時間差ではない」と正しく結論しているので、効果欄をその結論に合わせて書き換える |
| MEM-03 効果欄 | 「スライス方法の違いだけで **1.2〜1.5 倍程度の差**」と書いてあるが、同じ節の実測結果は「106.6 ns vs 107.0 ns(**信頼区間重複**)」。効果欄の数値には測定リンクが無い | コードサイズ根拠(103 vs 100 B)のみに整理する。あるいは「net10 では手書き不要になった最適化」表へ移す候補として扱う |
| 早見表の参照先 | `Unsafe.Add` / `MemoryMarshal.GetReference` / `GetArrayDataReference` / `Unsafe.IsAddressLessThan` の「関連パターン」欄が**すべて R-02(不採用)**を指しており、「構造上の用途のみ」と書きながらその用途を説明する本文が無い | 7-2 の判定結果で参照先を作る(採用なら STK-11、不採用なら R-12 の記述を訂正した上でそこを指す) |

---

## 🚫 今回のスコープ外

意図的に測らないもの。理由とセットで記録しておく。

| 項目 | 理由 |
|---|---|
| `Vector128.ShuffleNative`(.NET 9+) | ベンチマークプロジェクトが net8.0 も対象。methodology 落とし穴 7 を避け、採用後に別クラスで追加する |
| `MemoryMarshal.CreateReadOnlySpanFromNullTerminated` | ネイティブ文字列前提であり、R-19(P/Invoke をパターン化しない方針)のスコープ外 |
| `Vector256` / `Vector512` の幅比較 | VEC-01 で測定済み |
| 「壊れる Unsafe」の禁止表 | 測定対象ではなく文書の話。`MemoryMarshal.AsMemory(str).Span` による string 書き換え、呼び出し元の readonly を `Unsafe.AsRef(in x)` で剥がす形、`Unsafe.As<object[], T[]>` による共変配列チェック回避の 3 件。不採用一覧(効果が無い手法)とは別軸の表として、7-8 の結果と合わせて別途起票する |
| `ArrayPool<T>.Create` によるカスタムプール | ref / Unsafe 系ではないため本批次の対象外。BUF-01 の拡張候補として別途 |

---

## 📎 追加したファイル

| ファイル | 対応候補 |
|---|---|
| `benchmarks/PerformancePatterns.Benchmarks/Lab/ScopedRefBenchmark.cs` | 7-1 |
| `benchmarks/PerformancePatterns.Benchmarks/Lab/RefFieldStructReadBenchmark.cs` | 7-2 |
| `benchmarks/PerformancePatterns.Benchmarks/Lab/ValueRefLookupBenchmark.cs` | 7-3 |
| `benchmarks/PerformancePatterns.Benchmarks/Lab/StructLayoutBenchmark.cs` | 7-4 |
| `benchmarks/PerformancePatterns.Benchmarks/Lab/FalseSharingBenchmark.cs` | 7-5 |
| `benchmarks/PerformancePatterns.Benchmarks/Lab/MemoryAccessBenchmark.cs` | 7-6 / 7-10 / 7-13 |
| `benchmarks/PerformancePatterns.Benchmarks/Lab/UnsafeReinterpretBenchmark.cs` | 7-7 / 7-8 / 7-11 / 7-12 |
| `benchmarks/PerformancePatterns.Benchmarks/Lab/VectorShuffleBenchmark.cs` | 7-9 |
| `benchmarks/PerformancePatterns.Benchmarks/Program.cs`(変更) | `Verify()` 呼び出しと `BenchmarkSwitcher` への型登録を追加 |

net10.0 / net8.0 の Release ビルドで **警告 0・エラー 0** を確認済み。

---

# ✅ 判定結果(2026-08-22 実測)

**測定環境:** AMD Ryzen 9 5900X(Zen 3、12 物理 / 24 論理)、.NET 10.0.11、X64 RyuJIT **x86-64-v3(AVX2)**、MediumRun / LaunchCount=2、MemoryDiagnoser + DisassemblyDiagnoser。

> ⚠️ **既存の記録済み実測との環境差:** 本リポジトリの既存結果はすべて AMD Ryzen AI 9 HX 370(Zen 5)/ **x86-64-v4(AVX-512)** で取得されている。本批次は Zen 3 / AVX2 機での測定であり、絶対値および特にベクトル幅に依存する結論(VEC 系)は既存記録と直接比較できない。採用時は既存環境での再測定を行うか、環境を明記して併記する。

## 判定サマリー

| ID | 候補 | 判定 | 決め手 | 行き先 |
|:---:|---|:---:|---|---|
| 7-1 | `scoped` | ❌ **差なし**(性能) | caller / callee とも**命令列完全一致** | STK-01 の注記(安全性ツールとして) |
| 7-1 | `[UnscopedRef]` アクセサ | ❌ **不採用**(性能) | 1.07 倍・コード 85 → **88 B と増加**・命令数同じ | STK-01 の注記(ref を返す API を書く手段として) |
| 7-2 | ref フィールド構造読み | ✅ **採用** | **0.75 倍**(CI 非重複)・コード 163 → **111 B** | **STK-11** 新設 + R-12 と相互リンク |
| 7-3 | `GetValueRefOrNullRef`(更新) | ✅ **採用** | **0.48 / 0.62 / 0.51 倍**・コード **8,270 → 1,080 B** | **COL-07** 新設 |
| 7-3 | 同(読み取り) | ❌ **差なし** | 0.98〜1.00 倍・命令数一致・コードは増減混在 | COL-07 の「効かない側」として明記 |
| 7-4 | struct レイアウト | ✅ **条件付き採用** | 散在 **0.71 / 0.67 倍**(CI 非重複)、逐次は誤差 | **MEM-05** 新設(適用条件: 散在アクセス) |
| 7-5 | false sharing パディング | ✅ **採用** | **0.10〜0.14 倍**(最大 **29.7 倍**の罰則) | **CON-03** 新設 |
| 7-6 | `Memory<T>.Span` ホイスト | ✅ **採用** | 要素ごと **3.67 倍**・チャンクでも 1.10 倍(CI 非重複) | **BUF-08** 新設 |
| 7-7 | `Unsafe.BitCast` | ✅ **採用**(特例) | **命令列完全一致** = 乗り換えコストゼロを証明 | TYP-05 / JIT-03 / SEQ-02 本文へ既定として追記 |
| 7-8 | `MemoryMarshal.Cast` | ✅ **注記として採用** | Cast が 1.76 / 1.90 倍速い(既存推奨の裏付け) | 早見表 + BIT-04 / R-09 に条件追記 |
| 7-9 | 固定幅 Shuffle | ✅ **採用** | スカラー比 **0.46 倍**(移植可能形) | **VEC-02** 新設 |
| 7-9 | `Ssse3` vs `Vector128.Shuffle` | ❌ **API 差ではない** | **命令列完全一致**。1.76 倍は 64 B 境界跨ぎの配置由来(下記) | VEC-02 に「移植可能形を既定」と明記 |
| 7-10 | `MemoryManager<T>` | ✅ **採用** | ホイスト後は配列裏と同等、`.Span` 解決コードは **518 vs 297 B** | BUF-08 に同居 |
| 7-11 | ref から index 復元 | ❌ **不採用** | **1.45 倍 遅い**・コード 64 → 82 B | rejected-patterns へ(R-02 の系として) |
| 7-11 | `AreSame` / `Overlaps` | ✅ **注記として採用** | `AreSame` は `Overlaps` の 0.71 倍・89 vs 125 B | 早見表に 3 API 追加 |
| 7-12 | `Unsafe.Unbox` | ✅ **採用** | **0.18 倍**・割り当て **8,192 → 0 B** | STK-05 の拡張 |
| 7-13 | `MemoryMarshal.TryGetArray` | ✅ **採用** | 0.90 倍 + 割り当て **4,120 → 0 B**・コード 889 → 414 B | BUF-08 に同居 |

**採用 9 / 不採用 4(うち 2 は「差なし」確定)/ 注記 2。**

---

## 📊 実測

### 7-1 `scoped` / `[UnscopedRef]` — ❌ 性能パターンにはならない

| Method | Mean | Ratio | Code Size |
|---|---:|---:|---:|
| GetSetPair(基準) | 0.5013 ns | 1.00 | 85 B |
| UnscopedRefAccessor | 0.5349 ns | 1.07 | 88 B |
| PlainSpanParameter | 69.22 ns | — | 89 + 35 B |
| ScopedSpanParameter | 70.00 ns | — | 89 + 35 B |
| PlainRefParameter | 2.276 ns | — | 50 + 38 B |
| ScopedRefParameter | 2.296 ns | — | 50 + 38 B |

**`scoped`: 命令列完全一致(確定)。** 呼び出し側・呼び出し先とも逆アセンブリが 1 命令も違わず、差は `call` 先のシンボル名のみ。`SumSpan` / `SumScopedSpan` は 35 B で一致、`StepRef` / `StepScopedRef` は 38 B で一致。**`scoped` は純粋なコンパイル時契約であり codegen コストはゼロ** — 性能理由で付けるものでも、避けるものでもない。

**`[UnscopedRef]` アクセサ: 改善なし。** 逆アセンブリは別物になる(ref 返し側は `add [r8],r10` の 1 命令 read-modify-write に畳まれ、代わりに `lea` が増える)が、**命令数は同じ 7、コードサイズは 85 → 88 B と増加、時間も 1.07 倍**。時間・コードサイズ・命令数のどの軸にも改善がないため採用候補にならない。

**それでも文書化すべきこと:** STK-01 の注意が「ref struct には制約がある」で止まっており、回避手段を書いていない。`scoped`(呼び出し側の escape 制約を外す)と `[UnscopedRef]`(`ref this.field` を返せるようにする。付けないと CS8170 でコンパイル不可)は**性能パターンではなく、ref struct / ref 返し API を書くための必須知識**として STK-01 に追記する。

### 7-2 ref フィールド構造読み — ✅ 採用(STK-11)

| Method | Mean | Ratio | Code Size |
|---|---:|---:|---:|
| ParseInlineIndex(基準) | 1,038.8 ns | 1.00 | 163 B |
| ParseSpanReader | 994.0 ns | 0.96 | 153 B |
| ParseSliceReader | 842.3 ns | 0.81 | 128 B |
| **ParseRefFieldReader** | **782.5 ns** | **0.75** | **111 B** |

CI 非重複(761〜836 vs 988〜1,095)。**R-12 が「代わりにやること」で名指ししながら未測定だった用途が、実際に成立することを確認した。** 全要素走査(R-12、1.21 倍で敗北)と幅の違うフィールドを順に読む形状では結論が逆転する。

副次的な発見として、**再スライス型カーソル(`ParseSliceReader`、0.81 倍)も index 直書きに勝つ**。ref フィールドまで踏み込まなくても、可変長パースでは「残りを再スライスする」形が index 演算の直書きより速くコードも小さい。STK-11 では ref 版と再スライス版を段階として併記する。

### 7-3 `GetValueRefOrNullRef` — ✅ 更新経路のみ採用(COL-07)

| Probe | Method | Mean | Ratio | Code Size |
|---|---|---:|---:|---:|
| AllHit | TryGetValueRead(基準) | 2.327 μs | 1.00 | 1,055 B |
| AllHit | ValueRefOrNullRefRead | 2.287 μs | 0.98 | 1,071 B |
| AllHit | TryGetValueThenIndexerUpdate | 4.997 μs | 2.15 | **8,270 B** |
| AllHit | **ValueRefOrNullRefUpdate** | **2.401 μs** | 1.03 | **1,080 B** |
| HalfMiss | TryGetValueRead(基準) | 1.909 μs | 1.00 | 906 B |
| HalfMiss | ValueRefOrNullRefRead | 1.893 μs | 0.99 | 892 B |
| HalfMiss | TryGetValueThenIndexerUpdate | 3.185 μs | 1.67 | 8,051 B |
| HalfMiss | **ValueRefOrNullRefUpdate** | **1.967 μs** | 1.03 | 895 B |

32 バイト値(`ValueRefLargeLookupBenchmark`): 読み取り 2.486 → 2.485 μs(1.00)、更新 5.028 → **2.560 μs**。

**更新経路を 2 プローブ形と直接比較すると 0.48 / 0.62 / 0.51 倍。** 値サイズ 8 バイトでも 32 バイトでも同じ比率なので、**効いているのは値のコピー削減ではなくハッシュ探索の 1 回化**である。

**コードサイズの差が本質:** `dict[key] = value` のインデクサ setter は挿入経路(`TryInsert` / `Resize` / ハッシュヘルパー)ごと呼び出し側に展開され、**10 メソッド・8,270 B** に膨らむ。`GetValueRefOrNullRef` は既存スロットへの ref を返すだけなので **2 メソッド・1,080 B** で済む。7.7 倍のコードサイズ差はインライン化予算を通じて周辺コードにも波及する。

**読み取り経路は差なし。** 0.98〜1.00 倍で CI 重複、命令数も一致(AllHit で 199 vs 199)、コードサイズは +16 B / −14 B / −4 B と増減混在で一貫した優位がない。COL-07 には「読み取りだけなら `TryGetValue` のままでよい」と明記する。

### 7-4 struct レイアウト — ✅ 条件付き採用(MEM-05)

`Unsafe.SizeOf` は Verify で確定: **Padded 32 B / Packed 24 B / Auto 24 B**(`LayoutKind.Auto` は padded の宣言順を自動で詰める)。

| Method | Mean | Ratio | Code Size |
|---|---:|---:|---:|
| SequentialPadded(基準) | 14,840 ns | 1.00 | 65 B |
| SequentialPacked | 14,742 ns | 0.995 | 65 B |
| SequentialAuto | 14,345 ns | 0.968 | 65 B |
| **ScatteredPadded** | **26,213 ns** | 1.77 | 93 B |
| **ScatteredPacked** | **18,516 ns** | 1.25 | 93 B |
| **ScatteredAuto** | **17,662 ns** | 1.19 | 93 B |
| ByValuePadded | 2.034 ns | — | 102 B |
| ByValuePacked | 1.854 ns | — | 118 B |

**散在アクセスでは 32 B → 24 B が 0.71 / 0.67 倍**(CI 非重複)。逐次走査は 0.97〜1.00 倍で誤差 — プリフェッチャがフットプリント差を吸収するため。**生成コードは 3 型とも同一(65 B / 93 B)であり、差はコードではなくデータ側**にある。ここでは「生成コード一致 → 差なし」の判定規則は適用されない(規則はコード側の最適化を対象としたもの)ため、MEM-05 収録時にこの区別を明記する。

引数渡しは 2.034 → 1.854 ns(エラーバー非重複)。MEM-04 の「16 バイト超は `in`」に対して、**そもそもサイズを削れば値渡しのままでよくなる**という上流の選択肢を与える。

**収録内容の核:** C# の struct はメタデータ上**既定で `Sequential`** として出力されるため、宣言順を放置するとパディングが残る。対策は「広い順に自分で並べる」か「`[StructLayout(LayoutKind.Auto)]` を明示する」の二択で、実測上この 2 つは同等。

### 7-5 false sharing — ✅ 採用(CON-03)

| Workers | Method | Mean | Ratio |
|---:|---|---:|---:|
| 2 | AdjacentVolatile(基準) | 516.51 μs | 1.00 |
| 2 | Padded64Volatile | 69.43 μs | **0.14** |
| 2 | Padded128Volatile | 66.86 μs | **0.13** |
| 2 | AdjacentInterlocked | 646.38 μs | 1.28 |
| 2 | Padded128Interlocked | 415.88 μs | 0.82 |
| 4 | AdjacentVolatile(基準) | 853.96 μs | 1.00 |
| 4 | Padded64Volatile | 93.73 μs | **0.11** |
| 4 | Padded128Volatile | 92.94 μs | **0.11** |
| 4 | AdjacentInterlocked | 1,680.32 μs | 1.97 |
| 4 | Padded128Interlocked | 444.70 μs | 0.52 |
| 8 | AdjacentVolatile(基準) | 1,565.20 μs | 1.00 |
| 8 | Padded64Volatile | 150.59 μs | **0.10** |
| 8 | **Padded128Volatile** | **52.66 μs** | **0.03** |
| 8 | AdjacentInterlocked | 6,612.19 μs | 4.23 |
| 8 | Padded128Interlocked | 673.50 μs | 0.43 |

**本批次で最大の改善幅。** 隣接スロットへの並行書き込みは 2 ワーカーでも既に 7.4 倍、8 ワーカーで **29.7 倍**(1,565.20 / 52.66)の罰則を払う。

**64 バイトでは足りない。** ワーカー 2 / 4 では 64 B と 128 B が同等だが、**8 ワーカーでは 64 B が 150.59 μs、128 B が 52.66 μs で 2.86 倍の差**が開く。BCL の `PaddingHelpers` が 128 バイトを採用している理由(隣接キャッシュラインのプリフェッチ)が実測で確認できた。**CON-03 は 128 バイトを既定として書く。**

**interlocked でも消えない。** ロックプレフィックスが罰則を隠すどころか増幅する — 8 ワーカーで隣接 interlocked は隣接 volatile の 4.23 倍、パディング版との比は 9.8 倍(6,612 / 673)。CON-01(Interlocked ワンショットガード)は単一変数なので影響しないが、**カウンタ配列に Interlocked を使う設計では CON-03 が前提条件**になる。

割り当て(1.8〜3.1 KB)は `Parallel.For` の内部によるもので全 variant にほぼ等量(比 0.98〜1.00)乗るため、比較には影響しない。

### 7-6 / 7-10 / 7-13 `Memory<T>` — ✅ 採用(BUF-08)

| Method | Mean | Ratio | Code Size |
|---|---:|---:|---:|
| ArraySpanHoistedChunks(基準) | 1.281 μs | 1.00 | 248 B |
| ArrayMemorySliceChunks | 1.409 μs | 1.10 | 268 B |
| ArraySpanHoistedPerElement | 1.030 μs | 0.81 | 201 B |
| **ArraySpanPerElement** | **3.783 μs** | **2.96** | 178 B |
| ManagerSpanHoistedChunks | 1.244 μs | 0.97 | 297 B |
| ManagerMemorySliceChunks | 1.441 μs | 1.13 | **518 B** |

**要素ごとに `.Span` を解決すると、ホイストした場合の 3.67 倍**(3.783 / 1.030)。チャンク単位でも 1.10 / 1.13 倍で CI 非重複。

**裏の実体はホイスト後には影響しない**(Manager 1.244 ≒ Array 1.281)が、**`.Span` 解決コードのサイズは 518 vs 297 B と 1.7 倍**になる。ループ内で触ると効いてくるのはこの部分。

| Method | Mean | Ratio | Code Size | Allocated |
|---|---:|---:|---:|---:|
| ToArrayCopy(基準) | 1.715 μs | 1.00 | 889 B | **4,120 B** |
| **TryGetArraySegment** | **1.545 μs** | **0.90** | **414 B** | **0 B** |
| ManagerFallbackCopy | 1.804 μs | 1.05 | 1,255 B | 4,120 B |

`MemoryMarshal.TryGetArray` は時間 0.90 倍(CI 非重複)に加え**割り当てを丸ごと消す**。配列裏でない `Memory` では `TryGetArray` が false を返し、コピー退避が必要になる(Verify で固定済み)。

### 7-7 `Unsafe.BitCast` — ✅ 採用(事前登録の特例)

| Method | Mean | Ratio | Code Size |
|---|---:|---:|---:|
| UnsafeAsReinterpret(基準) | 241.5 ns | 1.00 | 57 B |
| UnsafeBitCastReinterpret | 243.9 ns | 1.01 | 57 B |
| BitConverterReinterpret | 243.6 ns | 1.01 | 57 B |
| GenericUnsafeAs | 237.5 ns | 0.98 | 21 B |
| GenericBitCast | 239.7 ns | 0.99 | 21 B |

**5 形すべて命令列が完全一致(確定)。** 具象型 3 形は同じ 57 B、ジェネリック 2 形は同じ 21 B。ジェネリック側は `T = int` で再解釈そのものが消滅し `movsxd` + `add` のループだけが残る。

判定フローの「命令列一致 → 差なし → 不採用」ではなく、**事前登録した特例に従い採用**する。判断軸が速度ではなく安全性(`Unsafe.As<TFrom,TTo>` はサイズ不一致を黙って受け入れるが `BitCast` は拒否する)であり、**命令列一致は「乗り換えても一切損しない」ことの証明**にあたるため。

**副次的な結論:** `BitConverter.SingleToInt32Bits` も同一コードに畳まれる。**具体的な型ペアに BCL API があるなら `Unsafe` を使う理由はまったくない** — これを TXT / TYP の記述に加える。

### 7-8 `MemoryMarshal.Cast` — ✅ 注記として採用

| Method | Mean | Ratio | Code Size |
|---|---:|---:|---:|
| MemoryMarshalCast(基準) | 243.7 ns | 1.00 | 57 B |
| UnsafeReadUnaligned | 428.3 ns | 1.76 | 54 B |
| BinaryPrimitivesRead | 463.2 ns | 1.90 | 103 B |

Cast は既存推奨どおり最速で、**手動 ref 走査(`ReadUnaligned` + `Unsafe.Add`)が 1.76 倍遅い**のは R-02 と同じ理由(索引形は境界チェック除去 + 自動ベクトル化が効く)。新パターンにはならないが、R-02 の適用範囲を「再解釈」まで広げる数値として使える。

**Verify で固定した落とし穴(こちらが成果物):**

- 10 バイトを `Cast<byte, int>` → 長さ **2**、**末尾 2 バイトが黙って消える**
- `int[3]` を `Cast<int, byte>` → 長さ **12**(この方向に int オーバーフローのガードがある)
- アラインメント検査は行われない(Arm で `DataMisalignedException` になりうる)

早見表の `MemoryMarshal.Cast` 行と、BIT-04 / R-09 の「ゼロコスト」記述に条件として追記する。

### 7-9 固定幅 Shuffle — ✅ 採用(VEC-02)。ただし API 差ではない

| Method | Mean | Ratio | Code Size |
|---|---:|---:|---:|
| ScalarReverse(基準) | 266.46 ns | 1.00 | 145 B |
| Vector128ShuffleReverse | 121.53 ns | **0.46** | 190 B |
| Ssse3ShuffleReverse | 64.35 ns | **0.24** | 190 B |
| VectorArithmeticReverse | 131.78 ns | 0.50 | 333 B |

**シャッフルはスカラーに大差で勝つ(0.46 倍)** — VEC-01 が「固定幅に落とすのは特定レーン構成が要るときだけ」と書きながら数値を持っていなかった部分が埋まった。`Vector<T>` の算術版(シフトとマスクで置換を組み立てる)は 0.50 倍で、**シャッフルが使えない場合でもほぼ同等まで到達する**が、コードサイズは 333 vs 190 B と 1.75 倍になる。

**`Ssse3.Shuffle` が `Vector128.Shuffle` の 1.89 倍速く見えるのは API の差ではない。** 以下 4 段階で確定した。

1. **逆アセンブリが完全一致** — 両者とも 190 B、`vpshufb xmm1,xmm1,xmm0` を含む同一命令列。差は定数マスクのアドレスのみ
2. **単独実行でも同じ差**(121.2 vs 69.5 ns)— 他ベンチマークとの相互作用ではない
3. **宣言順を入れ替えても入れ替わらない** — ソース上の位置には依存しない
4. **同一ソースの複製を追加すると、複製は元と同じアドレス・同じ時間になる** — `Vector128ShuffleReverseB` 124.41 ns / loop_start `…9FBC`、`Ssse3ShuffleReverseB` 65.69 ns / loop_start `…9F5C`。**メソッド本体が同一でも、どちらの API を書いたかでコードヒープ上の配置が決定的に変わる**

ホットループの配置:

| 形 | loop_start | ループ範囲 | 64 バイト境界 |
|---|---|---|---|
| `Ssse3.Shuffle` | `…9F5C` | `9F5C`〜`9F78`(28 B) | `[9F40, 9F80)` に**収まる** |
| `Vector128.Shuffle` | `…9FBC` | `9FBC`〜`9FD8`(28 B) | `9FC0` を**跨ぐ** |

**結論: 移植可能な `Vector128.Shuffle` を既定にしてよい。** 生の ISA 組み込み関数(`Ssse3`)に落とす理由は性能ではない。VEC-02 にはこの切り分けごと記録し、「1.89 倍速い」という誤った読み取りを封じる。

### 7-11 ref の同一性 / 位置 — ❌ index 復元は不採用、別名検査は注記

| Method | Mean | Ratio | Code Size |
|---|---:|---:|---:|
| IndexCarried(基準) | 560.1 ns | 1.00 | 64 B |
| IndexRecoveredFromRef | 811.0 ns | **1.45** | 82 B |
| AliasCheckWithAreSame | 714.1 ns | — | 89 B |
| AliasCheckWithOverlaps | 1,001.5 ns | — | 125 B |

**`Unsafe.ByteOffset` による index 復元は 1.45 倍遅く、コードも大きい。** 事前予想どおりで R-02 と同じ結論 — rejected-patterns へ「ref から index を戻すくらいなら index を持ち回る」として記録する。

別名検査は `Unsafe.AreSame`(先頭 ref 同士)が `Overlaps` の **0.71 倍**・コード 89 vs 125 B。ただし**意味が違う**(`AreSame` は先頭が同一か、`Overlaps` は範囲が重なるか)ので置換ではない。早見表に 3 API を追加し、この使い分けを添える。

### 7-12 `Unsafe.Unbox` — ✅ 採用(STK-05 拡張)

| Method | Mean | Ratio | Code Size | Allocated |
|---|---:|---:|---:|---:|
| UnboxCopyRebox(基準) | 1,601.5 ns | 1.00 | 582 B | **8,192 B** |
| **UnsafeUnboxInPlace** | **286.5 ns** | **0.18** | **251 B** | **0 B** |

3 軸すべてで改善(時間 5.6 倍・コード 0.43 倍・割り当てゼロ)。Verify で**ボックスのインスタンスが差し替わらないこと**も確認済みで、同一性を保つ点が再ボックス形との本質的な差。STK-05 は「ボックス化させない」話なので、**「既に存在するボックスを更新する」**場合の追記として収録する。

---

## 🔬 副産物: コードサイズ一致 ≠ 同一性能(方法論への追加候補)

7-9 の切り分けで、**命令列が完全に一致していてもホットループが 64 バイト境界を跨ぐと 1.76 倍の実差が出る**ことを実測した。これは benchmark-methodology の判断基準に直接影響する。

現行の規則は「信頼区間が重なった場合、生成コードが一致すれば**差なし**として不採用」である。この規則自体は正しい(前提が「計測でも差がない」場合だから)。しかし**逆向きの推論 —「生成コードが一致するのだから性能も同じはず」— は成立しない**。7-9 はまさにその反例で、命令列一致にもかかわらず 1.76 倍の再現性ある差が出ている。

方法論に追加すべき項目(落とし穴 10 の候補):

> **10. 同一命令列でも配置で 2 倍動く**
> ホットループが 64 バイト境界を跨ぐかどうかで、命令列が完全一致していても 1.76 倍の再現性ある差が出る(VEC-02 の検証で実測)。「コードサイズが同じ = 同じ性能」とは限らない。予想外の差が出て逆アセンブリが一致した場合は、**同一ソースの複製メソッドを追加して測る** — 複製が元と同じ時間になれば配置由来ではなく、違う時間になれば配置由来と切り分けられる。宣言順の入れ替えでは配置は動かない(JIT のコードヒープ配置は宣言順に依存しないため)ので、切り分け手段にならない。

---

## 📌 次にやること

1. **採用 9 件を README 本文へ収録** — 新設 ID は MEM-05 / STK-11 / COL-07 / CON-03 / BUF-08 / VEC-02 の 6 つ、既存拡張は STK-05(Unbox)/ TYP-05・JIT-03・SEQ-02(BitCast)/ STK-01(scoped・UnscopedRef の注記)
2. **不採用 2 件を rejected-patterns へ** — `[UnscopedRef]` アクセサ(R-20 案)、`ByteOffset` による index 復元(R-21 案、R-02 の系)
3. **注記 2 件を早見表へ** — `MemoryMarshal.Cast` の切り捨て・アラインメント、`AreSame` / `ByteOffset` / `Overlaps` の追加
4. **方法論へ落とし穴 10 を追加**(上記)
5. **既存の不整合 3 件を修正**(本文書「併せて直す既存の不整合」)
6. **本批次を Zen 5 / AVX-512 機で再測定するか、環境を明記して併記する** — 特に VEC-02 と CON-03(キャッシュライン挙動)は世代依存
7. 測定結果を `benchmarks/results/` の個別ファイルへ切り出す(採用が確定した ID ごと)。生の BDN 出力は `benchmarks/PerformancePatterns.Benchmarks/BenchmarkDotNet.Artifacts/results/` にある

---

## 🚚 反映状況(2026-08-22)

### ✅ 完了

| 項目 | 内容 |
|---|---|
| 新設 6 ID の本文執筆 | MEM-05 / STK-11 / BUF-08 / VEC-02 / COL-07 / CON-03 を README.ja.md・README.md の両方へ収録(パターン一覧・逆引き・早見表の行も追加)。アンカー 113 件・ファイルリンクとも壊れなしを検証済み |
| 測定結果ファイル | `benchmarks/results/` に 6 件を追加(VEC-02 には配置検証の複製メソッド表も併記) |
| 不整合 ①(MEM-02 効果欄) | 「走査中心でも約 1.5 倍」→ 実測(3% 以内)に合わせ、採用理由を構造面に書き換え |
| 不整合 ②(MEM-03) | 効果欄の「1.2〜1.5 倍」を削除し「時間差は分解できない」に。目的欄も「コストを削減」から「コードを引き締める(時間差はない)」へ |
| 不整合 ③(早見表の参照先) | `Unsafe.Add` / `IsAddressLessThan` / `GetReference` の関連パターンが R-02(不採用)しか指していなかった問題を、STK-11 / VEC-02 を指すよう修正。`GetArrayDataReference` は正の用途がないことを明記 |
| 早見表の追加行 | `TryGetArray` / `MemoryManager<T>`(BUF-08)、`GetValueRefOrNullRef` / `Unsafe.IsNullRef`(COL-07) |

### ✅ 完了(第 2 陣)

| 項目 | 内容 |
|---|---|
| 既存パターンの拡張 | STK-01 に `scoped` / `[UnscopedRef]` の注記、STK-05 に `Unsafe.Unbox`(実装例・実測・注意つき)、JIT-03 / TYP-05 / SEQ-02 に `Unsafe.BitCast` を既定とする追記 |
| 不採用一覧 | R-20(`[UnscopedRef]` ref 返しアクセサ)/ R-21(`ByteOffset` による index 復元)を README の表と rejected-patterns の本文へ追加。あわせて表から漏れていた R-19 も追加 |
| 早見表 | `Unsafe.AreSame` / `Unsafe.ByteOffset` / `MemoryExtensions.Overlaps` / `Unsafe.Unbox<T>` を追加。`BitCast` 行を「サイズ不一致を拒否する `As` の安全版・生成コードは同一」に、`Cast` 行に切り捨て・アラインメント無検査の注記を追加 |
| BIT-04 / R-09 | 「`Cast` はゼロコスト」の記述に、長さ変化・端数切り捨て・アラインメント無検査の条件を追記 |
| 方法論 | 落とし穴 10「同一命令列でも配置で 2 倍動く(コードサイズ一致 ≠ 同一性能)」と、切り分け手順(同一ソースの複製メソッドを測る/宣言順の入れ替えは無効)を追加 |
| 検証キュー | 批次⑦の 13 行を [benchmark-methodology](benchmark-methodology.ja.md) へ転記。➖誤差・差なし記録にも 5 件(scoped / UnscopedRef / BitCast / ValueRef 読み取り / VEC-02 の配置)を追加 |
| 補助の測定記録 | `LAB-ScopedRef` / `LAB-BitCast` / `LAB-SpanReinterpret` / `LAB-RefIdentity` / `LAB-UnboxInPlace` を `benchmarks/results/` に追加 |

**検証:** README 日英とも 4,097 行で一致、見出しアンカー 113 件・ファイルリンクとも破損 0、全編集ファイルの CRLF を維持。ベンチマークプロジェクトは Release(net10.0 / net8.0)で警告 0・エラー 0。

### 📌 残っているもの

| 項目 | 内容 |
|---|---|
| 環境 | 本批次の実測はすべて **x86-64-v3(Zen 3 / Ryzen 9 5900X)** で取得している。本書の他項目は **x86-64-v4(Zen 5 / Ryzen AI 9 HX 370)**。新設 6 ID の実測欄には `x86-64-v3` と明記し、VEC-02 と CON-03 には環境依存の注記を入れてあるが、**同一環境での再測定が望ましい**(特にキャッシュライン挙動とベクトル幅) |
| 本ファイル | 内容はすべて README・rejected-patterns・benchmark-methodology・`benchmarks/results/` へ移送済み。冒頭の方針どおり削除してよい。VEC-02 の配置調査の詳細は [VEC-02-VectorShuffle.md](../benchmarks/results/VEC-02-VectorShuffle.md) と方法論の落とし穴 10 に残る |
