# 本検証の引き継ぎ(HX 370 環境用)— リンク取り込みバッチ 2026-09

このファイルは、B550H(Ryzen 9 5900X / x86-64-v3)で**仮実行**した 7 本のベンチマークを、
カタログの基準環境 HX 370(Ryzen AI 9 HX 370 / x86-64-v4)で**本実行**し、ドキュメントを確定させるための手順書。
完了後はこのファイルを削除してよい。

## そのまま貼れるプロンプト

```
dotnet-performance リポジトリで、docs/verification-handoff.ja.md の手順に従って本検証を行なってください。

背景: 外部記事 5 件の取り込み判断のために 7 本の LAB ベンチマークを追加し、B550H(x86-64-v3)で仮実行済みです。
ドキュメント内の「⏳」が仮値、「⏳❗」が「出典の主張と食い違った想定外の結果」で、後者は本検証の結果を見てから採否を確定します。

やること:
1. git pull 後、benchmarks/PerformancePatterns.Benchmarks で `dotnet run -c Release --framework net10.0 -- --list flat` を実行し、
   一覧が出ること(= Program.cs の Verify 群が全て通ること)を確認
2. 手順書の「実行コマンド」で 7 本を実行(約 30 分)
3. 手順書の「判定表」に従い、❗ 5 件の採否を確定。判定が仮判定と異なる場合は本文を書き換える
4. 手順書の「差し替え箇所」に従い、⏳ / ⏳❗ の数値と文言を HX 370 の値へ差し替え、マーカーを全て除去
5. benchmarks/results/LAB-*.md 7 本を HX 370 の結果で作り直し、冒頭の仮測定バナーを外す
6. README.ja.md / README.md、docs/rejected-patterns.ja.md / .md、docs/benchmark-methodology.ja.md / .md の
   行数と見出し行番号が ja / en で一致していること、全ファイル CRLF、ソリューションが 0 警告でビルドできることを確認
7. 完了報告では「仮判定から変わった項目」を先頭に書く

判定は時間よりも生成コード(コードサイズ・RNGCHKFAIL の有無)と確保バイト数を優先してください。
このリポジトリの AGENTS.md(警告抑制は要確認、CRLF 維持)に従ってください。
```

## 実行コマンド

```bash
cd benchmarks/PerformancePatterns.Benchmarks
dotnet run -c Release --framework net10.0 -- --filter "*EnumerableEscape*" "*DelegateEscape*" "*BoundsCheckPattern*" "*Int32Parse*" "*HashTableDesign*" "*InlineList*" "*ReadOnlyCollectionLoop*"
```

結果は `BenchmarkDotNet.Artifacts/results/PerformancePatterns.Benchmarks.Lab.*-report-github.md`(数値)と `*-asm.md`(逆アセンブル)。
B550H では 30 本で約 30 分だった。

## 判定表 — ❗ 5 件はここで確定する

| # | ベンチマーク | 出典の主張 | B550H 仮結果 | HX 370 で確認すること | 確定ルール |
|---|---|---|---|---|---|
| 1 | `DelegateEscapeBenchmark` | .NET 10 はエスケープしないデリゲートをスタック化(88 B → 24 B/回) | `CapturingLoopLocal` **5,632 B = 64 × 88 B、消去なし** | `CapturingLoopLocal` の Allocated | **1,536 B(= 64 × 24 B)なら消去あり** → DSP-04 の段落を「デリゲートは消えるが表示クラス 24 B は残る」に書き換え。**5,632 B のままなら仮判定どおり**「消去せず、指針不変」で確定 |
| 2 | `ReadOnlyCollectionLoopBenchmark` | .NET 10 で `ReadOnlyCollection<int>` の foreach が添字を上回る | foreach **1.26 倍遅・32 B 確保・684 B** | `Foreach` の Allocated と比率 | **0 B かつ添字以下なら逆転あり** → R-04 の段落を書き換え、ラッパーでも foreach 可とする。**32 B が残るなら仮判定どおり**「添字のまま」で確定 |
| 3 | `Int32ParseBenchmark` | ポインタ 0.19 倍 vs Span 0.26 倍(ポインタ優位) | **Cast 0.26 / ポインタ 0.52**(Cast が 2 倍速い) | `Pointer` と `SpanCast` の比率・コードサイズ | **Cast ≤ ポインタなら R-09 の補強で確定**。ポインタが Cast より速ければ R-09 本文の「Cast がポインタの 2 倍速い」を実測値に直し、「同速か遅い」という R-09 の主張自体を見直す |
| 4 | `HashTableDesignBenchmark` | Ankerl 方式が Dictionary の 3.8 倍高速 | **1.26〜1.66 倍遅い、同一ハッシュでも 1.40 倍遅い** | `AnkerlHit` vs `DictionaryHit` / `DictionaryOrdinalHit` | **Ankerl が遅いままなら R-22 を不採用で確定**(「仮判定」の文言を外す)。**Ankerl が速ければ R-22 を取り下げ**、COL 系の採用候補として別途起票 |
| 5 | `InlineListBenchmark` | 収まる範囲で List の 166% 高速 | 収まる: **0.35 倍・0 B**。溢れる: **容量指定 List に 1.62 倍負け** | `Items=32` の `InlineListStruct` vs `ListWithCapacity` | **溢れると負けるなら「条件付き採用(上限既知のみ)」で確定**。溢れても勝つなら条件を外して STK-08 の派生として無条件採用 |

想定どおりで数値差し替えのみの 2 件:

| ベンチマーク | 確認すること | 想定 |
|---|---|---|
| `EnumerableEscapeBenchmark` | `InterfaceStatic` の Allocated が 0 B、`InterfaceMixed` が確保あり | STK-03 の「単相なら 0 B、多相で戻る」を数値差し替え |
| `BoundsCheckPatternBenchmark` | asm で `CharAfterPrefix(ReadOnlySpan…)` にのみ `CORINFO_HELP_RNGCHKFAIL` があること、`SumIfFour` / `SumSwitchFour` にないこと | R-15 の表を数値差し替え。asm が変わっていたら表の「消える / 残る」も直す |

## 差し替え箇所(`⏳` で grep すれば全て出る)

| ファイル | 箇所 | 内容 |
|---|---|---|
| `README.ja.md` / `README.md` | STK-03(.NET 10 での前提の変化) | ⏳ 表 4 行の時間・確保・コードサイズ |
| 〃 | DSP-04(.NET 10 のエスケープ解析) | ⏳❗ 表 3 行の確保バイト数。判定表 #1 の結果で本文を確定 |
| 〃 | STK-08(派生: InlineList) | ⏳❗ 表 2 行の時間・確保。判定表 #5 で条件を確定 |
| 〃 | 不採用一覧の R-22 行 | ⏳❗「仮判定(不採用見込み)」を確定文言へ |
| `docs/rejected-patterns.ja.md` / `.md` | R-04(ラッパーコレクション) | ⏳❗ 段落。判定表 #2 |
| 〃 | R-09(📌 外部報告の検証) | ⏳❗ 表 4 行。判定表 #3 |
| 〃 | R-15(📌 .NET 10 での範囲) | ⏳ 表のコードサイズ・倍率 |
| 〃 | R-22 全体 | ⏳❗ 表 3 行。判定表 #4 で「仮判定」を外すか取り下げ |
| `docs/benchmark-methodology.ja.md` / `.md` | 判定表の 6 行(⏳ 2 行 + ⏳❗ 4 行) | 倍率と判定記号 |
| `benchmarks/results/LAB-*.md` × 7 | 冒頭バナー + 表 | 環境ヘッダごと HX 370 の出力で作り直し、バナー(⏳ / ❗)を削除 |

## 完了条件

- `grep -rn "⏳" README.ja.md README.md docs/ benchmarks/results/` で残るのが `docs/benchmark-methodology.*.md` の既存行(「⑥ net11 世代ウォッチ」)だけになっている
- ja / en の行数と `### ` 見出しの行番号が一致(README・rejected-patterns・benchmark-methodology)
- 全対象ファイルが CRLF(`grep -c $'\r$'` が総行数と一致)
- `dotnet build dotnet-performance.slnx -c Release` が 0 警告
- 判定が仮判定から変わった項目があれば、その旨を `docs/benchmark-methodology.ja.md` の該当行の判定欄に「仮判定から変更」と 1 語添える
