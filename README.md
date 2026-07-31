# dotnet-performance

高速・低アロケーションにチューニングされた .NET ライブラリを実装するためのノウハウ集。

パターンカタログと、パターンごとの具体的な実装例・ベンチマークを提供する。
ライブラリ開発時に AI へこのリポジトリを参照させることで、高性能かつ AOT 対応の実装を再現できる状態にすることを目的とする。

## ドキュメント

| ドキュメント | 内容 |
|---|---|
| [docs/performance-patterns.md](docs/performance-patterns.md) | パフォーマンス実装パターン一覧(パターン ID・AOT 対応可否・反パターン付き) |
| [docs/aot-compatibility.md](docs/aot-compatibility.md) | AOT / トリミング対応パターン一覧(非互換パターンと対策パターン) |
| [docs/benchmark-methodology.md](docs/benchmark-methodology.md) | ベンチマーク実施ガイドライン(BenchmarkDotNet 構成と測定の落とし穴) |

## パターン ID 体系

実装例プロジェクト・ベンチマークはパターン ID に対応付けて追加する。

| プレフィックス | カテゴリ |
|---|---|
| MEM | メモリアクセス最適化(境界チェック除去・データレイアウト) |
| JIT | JIT 最適化支援(インライン化・分岐除去・特殊化) |
| BIT | ビット演算・ブランチレス最適化 |
| DSP | 呼び出し抽象化・ディスパッチ |
| STK | スタック活用・ゼロアロケーション型設計 |
| BUF | バッファ管理・プーリング |
| SEQ | 逐次読み書き・シーケンス処理 |
| COL | コレクション最適化 |
| TXT | 文字列・フォーマット |
| TYP | 型システム活用(型ディスパッチ・比較・内部アクセス) |
| AOTP | AOT 非互換パターン(避けるべき実装) |
| AOTS | AOT 対策パターン(ライブラリ設計手法) |

## 構成(予定)

```
dotnet-performance/
├── docs/         パターンカタログ
├── src/          パターンごとの実装例(パターン ID に対応)
└── benchmarks/   BenchmarkDotNet によるベンチマーク
```

## ロードマップ

1. [x] パターン一覧の整備
2. [ ] パターンごとの実装例プロジェクトの作成
3. [ ] ベンチマークによる効果の実証
4. [ ] Source Generator を含む AOT 対応実装例の作成
5. [ ] 拡充候補パターン(SearchValues / FrozenDictionary / SIMD 等)のドキュメント化
