# 🏭 Source Generator 生成コードパターン集

**日本語** | [English](generated-code-patterns.md)

Source Generator で**どのようなコードを生成すればパフォーマンスを実現できるか**のカタログ。
ジェネレータの実装方法(Roslyn API)ではなく、**出力すべきコードの形**と、その形が速いことの実測根拠を記録する。
本書のコード例はすべて「ジェネレータの出力イメージ」であり、同じ形を手書きしても同じ性能になる(実測は手書き形で取得済み)。

対応する本体パターン: [README](../README.md) の **GEN-02**。AOT 文脈の位置づけは [aot-compatibility.md](aot-compatibility.md) の **AOTS-01**(根本対策)と **AOTS-08**(二重パス)。

---

## 🧭 生成コード設計の 3 原則

1. **実行時解決をビルド時へ移す** — 辞書引き・リフレクション・ハッシュ計算・文字列組み立てのうち、生成時に確定できるものはコード(定数・switch・直書き `new`)に焼き込む
2. **件数・形状で出し分ける** — ジェネレータは対象の件数・型・レイアウトを知っている。実行時ライブラリにはできない「N に応じた実装の切り替え」を生成時に行う
3. **測定済みパターンの組み合わせで出力する** — 生成コードの中身は本カタログの採用パターンで構成し、不採用パターン(R-01〜R-17)を含めない

---

## 1. 名前 → インデックス解決(列名・プロパティ名・キー文字列)

**シナリオ:** DB 列名、プロパティ名、JSON キーなど「既知の文字列集合 → 番号」の解決。

**生成すべきコード:** 件数で出し分ける。

```csharp
// 件数 ≤ 4: Equals 連鎖(生成イメージ)
public static int GetIndex(ReadOnlySpan<char> name)
{
    if (name.SequenceEqual("Id")) return 0;
    if (name.SequenceEqual("Name")) return 1;
    if (name.SequenceEqual("Flag")) return 2;
    return -1;
}

// 件数 ≥ 5: サンプリングハッシュ switch(ハッシュ定数は生成時に計算して焼き込む)
public static int GetIndex(ReadOnlySpan<char> name)
{
    switch (SamplingHash.Calculate(name))   // (length << 16) ^ (first << 8) ^ (mid << 4) ^ last
    {
        case 0x00243C56 when name.SequenceEqual("CreatedAt"): return 2;
        case 0x00159F11 when name.SequenceEqual("Name"): return 1;
        // ...
    }
    return -1;
}
```

**なぜ速いか:** 全文字を読む一般ハッシュと違い、長さ + 3 文字のサンプリングで候補を絞り、確定比較は SIMD 化された `SequenceEqual` 1 回。ハッシュ定数が JIT 定数になるため switch はジャンプテーブル化される。

**実測の裏付け:**

- サンプリングハッシュ表(実行時版)が `Dictionary` の 0.56〜0.84 倍、Span キーでは `FrozenDictionary` にも全サイズで勝つ → [COL-04-SampledNameTable.md](../benchmarks/results/COL-04-SampledNameTable.md)
- 線形探索(Equals 連鎖の実行時版)が勝つのは 4 件まで、16 件で 2.73 倍に劣化 → 同上(出し分け閾値の根拠)
- 生成時に位置を選べるため、衝突するキー集合ではサンプリング位置の変更で回復できる(実行時版にはできない生成ならではの自由度)

**注意:** 大文字小文字を無視する場合はサンプリング文字を大文字化して計算し、確定比較を `OrdinalIgnoreCase` にする。比較は常に序数系(TXT-03)。

---

## 2. 型別成果物の焼き込み(SQL 断片・型名・書式文字列)

**シナリオ:** 型ごとに決まる SQL、ログ用型名、シリアライズのキー名など。

**生成すべきコード:** 実行時に組み立てず、**const / static readonly へ直書き**する。

```csharp
// 生成イメージ: 実行時の StringBuilder も辞書引きも存在しない
internal static class OrderSql
{
    public const string Insert = "INSERT INTO Order (Id, Name, Amount, CreatedAt) VALUES (@Id, @Name, @Amount, @CreatedAt)";
}

// UTF-8 が必要なら u8 リテラルで焼き込む(実行時エンコードなし)
internal static class OrderJson
{
    public static ReadOnlySpan<byte> IdKey => "\"id\":"u8;
}
```

**なぜ速いか:** 読み出しは定数ロードのみ。`ReadOnlySpan<byte>` プロパティ + u8 リテラルはアセンブリのデータ領域を直接指すため確保ゼロ。

**実測の裏付け:** 毎回組み立て 116 ns + 760 B → 辞書キャッシュ 4.8 ns → **ジェネリック static 読み 0.09 ns / コードサイズ 6 B**。生成コードは最速の形(static 読み)をさらに const へ倒せる → [TYP-06-StaticArtifact.md](../benchmarks/results/TYP-06-StaticArtifact.md)

**注意:** ジェネリック型引数に依存する成果物は `static class Cache<T>` 形(TYP-04 / TYP-06)で生成する。型初期化子で例外を出さない設計にする。

---

## 3. DB 行マッパー

**シナリオ:** `DbDataReader` → POCO のマッピング(Dapper 系が実行時にやることをビルド時に)。

**生成すべきコード:** 序数 struct + 1 パス列解決 + 型別 getter。

```csharp
// 生成イメージ
private readonly struct OrderOrdinals(int id, int name, int flag)
{
    public readonly int Id = id;
    public readonly int Name = name;
    public readonly int Flag = flag;
}

public static OrderOrdinals ResolveOrdinals(DbDataReader reader)
{
    int id = -1, name = -1, flag = -1;
    for (var i = 0; i < reader.FieldCount; i++)
    {
        var column = reader.GetName(i);
        // 列数が多い場合はシナリオ 1 の名前スイッチを生成して使う
        if (string.Equals(column, "Id", StringComparison.OrdinalIgnoreCase)) { id = i; }
        else if (string.Equals(column, "Name", StringComparison.OrdinalIgnoreCase)) { name = i; }
        else if (string.Equals(column, "Flag", StringComparison.OrdinalIgnoreCase)) { flag = i; }
    }
    return new OrderOrdinals(id, name, flag);
}

public static Order Map(DbDataReader reader, in OrderOrdinals ordinals) => new()
{
    Id = reader.GetInt32(ordinals.Id),        // GetValue + キャストは生成しない(ボックス化)
    Name = reader.GetString(ordinals.Name),
    Flag = reader.GetBoolean(ordinals.Flag),
};
```

**なぜ速いか:** 列解決がリーダー 1 本につき 1 回になり、行ループは struct フィールド読み + 型別 getter 直呼びだけになる。

**実測の裏付け:** 毎行 `GetOrdinal` 11.3 ns/行 → **序数 struct + `in` 渡し 1.42 ns/行(0.13 倍)**、コードサイズ 2,225 → 537 B。`GetValue` + キャストを生成すると 7.18 ns/行 + **48 B/行のボックス化** → [DAT-01-OrdinalResolve.md](../benchmarks/results/DAT-01-OrdinalResolve.md)

**注意:** 欠落列を許すなら -1 のままにして Map 側で分岐を生成(`GetOrdinal` は例外を投げるため使わない)。enum 列は基底型で読んでキャストするコードを生成する。

---

## 4. ファクトリ / DI 解決

**シナリオ:** 型登録済みの依存グラフからインスタンスを構築する(DI コンテナが Emit でやることをビルド時に)。

**生成すべきコード:** **依存グラフを `new` の直書きへインライン展開**する。子ファクトリ呼び出しの連鎖を生成しない。

```csharp
// ✅ 生成イメージ: グラフを 1 メソッドに展開(シングルトンは static readonly 読み)
internal static class ServiceFactory
{
    private static readonly DepA SharedDepA = new();

    public static Service Create() => new(SharedDepA, new DepB(new DepC()));
}

// ❌ 生成してはいけない形: 子ファクトリを Func で持ち回って呼ぶ
public static Service Create() => new(
    (DepA)childFactories[0](),   // デリゲート呼び出し + castclass の連鎖
    (DepB)childFactories[1]());
```

**なぜ速いか:** 呼び出し連鎖・castclass・デリゲート間接がすべて消え、JIT がコンストラクタをインライン化できる直呼びになる。

**実測の裏付け:** GEN-01(Emit 側の同一シナリオ)で、子ファクトリ連鎖は直書き比 **2.3 倍**、closure 配列ターゲットは 1.5 倍のペナルティ。直書き相当(DirectLambda)は 6.23 ns → [GEN-01-EmitStrategy.md](../benchmarks/results/GEN-01-EmitStrategy.md)。生成コードは Emit の最良形(Holder フィールド 6.55 ns)と同等の形を、AOT 安全に出力できる。

**注意:** ライフタイム(シングルトン / 都度生成)は生成時に確定して形で表現する(シングルトン = static readonly、都度 = `new` 直書き)。実行時 `Type` からの解決が必要な入口だけ `Dictionary<Type, Func<object>>` を生成し、中身は上記の直書きファクトリを指す(TYP-01 の実行時 Type 経路が素の辞書より遅い実測に注意 — 型が静的に分かる呼び出しをジェネリック API で受けるのが先)。

---

## 5. 整形・シリアライズ

**シナリオ:** JSON・ログ・固定長など、値 → テキスト/バイナリの書き出しコード。

**生成すべきコード:**

```csharp
// ✅ 数値・日時は TryFormat 直呼び(中間 string を作らない)
value.TryFormat(destination, out written);

// ✅ 既知のキー・区切りは u8 リテラルの CopyTo(実行時エンコードなし)
"\"name\":"u8.CopyTo(destination);

// ✅ 長さが事前計算できる文字列連結は string.Create を生成
return string.Create(length, state, static (span, s) => { /* CopyTo の列 */ });

// ✅ 固定書式(日時など)は 2 桁テーブル参照を生成(TXT-01)
```

**なぜ速いか:** 中間 string / 中間 byte[] が消え、書き出しがバッファ直書きの列になる。

**実測の裏付け:**

- `string.Create` は補間の 0.57 倍・割り当ては結果のみ → [TXT-07-StringCreate.md](../benchmarks/results/TXT-07-StringCreate.md)
- 固定書式のテーブル化 → [TXT-01-Utf8DateTimeFormatter.md](../benchmarks/results/TXT-01-Utf8DateTimeFormatter.md)
- **手書きの桁詰めループを生成してはいけない**(`TryFormat` の 2.5〜4.8 倍遅い、R-16)→ [TXT-09-FixedFieldFormat.md](../benchmarks/results/TXT-09-FixedFieldFormat.md)

**注意:** 逐次書き込みの受け皿は `IBufferWriter<T>`(BUF-02)か Span 直書き(SEQ-02)を生成の既定にする。

---

## 6. enum 特化(TryParse / ToString)

**シナリオ:** 既知の enum に対する名前⇔値変換。

**生成すべきコード:** シナリオ 1(名前スイッチ)の適用 + ToString は switch 定数返し。

```csharp
// 生成イメージ
public static bool TryParse(ReadOnlySpan<char> name, out Color value)
{
    // 件数に応じて Equals 連鎖 / サンプリングハッシュ switch(シナリオ 1 と同型)
}

public static string FastToString(this Color value) => value switch
{
    Color.Red => "Red",       // 定数返し(実行時の名前解決・確保なし)
    Color.Green => "Green",
    _ => value.ToString(),    // 未知値は BCL へフォールバック
};
```

**実測の裏付け:** 名前解決部はシナリオ 1 と同型(COL-04 の実測に帰着)。ToString の定数返しは確保ゼロが構造的に保証される(`Enum.ToString` は文字列生成を伴う)。

**注意:** まず BCL の `Enum.TryParse<T>(ReadOnlySpan<char>, ...)` で足りるかを確認してから生成する。`(T)Enum.Parse(typeof(T), name)` 形のコードは生成しない(ボックス化、AOTP-05 系の懸念も)。

---

## 7. コレクション変換

**シナリオ:** 配列 / List / DB 結果 → DTO リストの変換コード。

**生成すべきコード:** 件数既知を前提に確保を確定させる。

```csharp
// 生成イメージ: 容量確定 + SetCount + Span 直書き(COL-01 / COL-06)
var list = new List<TDestination>(source.Length);
CollectionsMarshal.SetCount(list, source.Length);
var span = CollectionsMarshal.AsSpan(list);
for (var i = 0; i < source.Length; i++)
{
    span[i] = Convert(source[i]);
}
```

**実測の裏付け:** SetCount + Span 直書きは Add ループの 0.21〜0.27 倍・割り当てゼロ(再利用時)→ [COL-06-CollectionConvert.md](../benchmarks/results/COL-06-CollectionConvert.md)。連続領域からの `ImmutableArray` は `ToImmutableArray()` を生成(Builder 経由は 2.5〜3.4 倍遅い)。

---

## 8. 変更通知・イベント(INotifyPropertyChanged 生成など)

**シナリオ:** プロパティ変更通知、イベント発火コードの生成。

**生成すべきコード:**

```csharp
// ✅ PropertyChangedEventArgs は static readonly へ焼き込む(発火ごとの確保をゼロに)
private static readonly PropertyChangedEventArgs NameChangedArgs = new(nameof(Name));

public string Name
{
    get => name;
    set
    {
        if (!string.Equals(name, value, StringComparison.Ordinal))
        {
            name = value;
            PropertyChanged?.Invoke(this, NameChangedArgs);   // 確保なし
        }
    }
}
```

**なぜ速いか:** 発火のたびの `new PropertyChangedEventArgs(...)` が消える(構造的に確保ゼロ)。イベント購読構造を自前で生成する場合は購読者数で形を選ぶ。

**実測の裏付け:** 購読 1 個ならマルチキャストデリゲートが最速(配列形は 2.87 倍遅)、**2 個以上で不変配列形が逆転**(4 個で 0.36 倍)→ [DSP-03-HandlerList.md](../benchmarks/results/DSP-03-HandlerList.md)。コールバックは static ラムダ + TState 形で生成する(DSP-04)。

---

## 9. ❌ 生成してはいけないコード(アンチ生成リスト)

「速そうに見える」ために生成コードへ混入しがちだが、**実測・生成コード確認で効果なし〜逆効果と確定済み**の形。ジェネレータ(および AI のコード生成)はこれらを出力しないこと。

| 生成してはいけない形 | 理由(実測) | 記録 |
|---|---|---|
| `typeof(X)` の static readonly キャッシュ | Tier1 で生成コード完全一致。昇格前はキャッシュ側が不利ですらある | R-01 |
| 読み取り専用辞書の無条件 `FrozenDictionary` 化 | 構築 15〜20 倍、検索もキー集合次第で逆転 | R-08 |
| インスタンスフィールドへの性能目的 readonly | 生成コード同一(オフセット以外)を確認 | R-10 |
| `Span.CopyTo` の `Unsafe.CopyBlockUnaligned` 置換 | 可変長は同じ Memmove に到達(誤差)。安全性だけ失う | R-14 |
| 手書きの桁詰め整形ループ(右詰めシフト・逆順書き) | `TryFormat` + `Fill` の 2.5〜4.8 倍遅い | R-16 |
| デリゲート Invoke の `call` 置換(`callvirt` 回避) | 生成コード 68 命令・229 B 完全一致を JIT 確認 | R-17 |
| 単一 Span ループの `GetReference` + `Unsafe.Add` 化 | 標準 for で境界チェック除去済み。手動化は 1.07〜1.13 倍遅 + バグ源 | R-02 |
| 自前ハッシュループ(FNV-1a 等)の生成 | 64 文字以降 `string.GetHashCode` より遅い。XxHash3 かサンプリング(シナリオ 1)を使う | [BIT-04](../benchmarks/results/BIT-04-XxHash3.md) |
| 実行時 `Type` キー辞書を主経路にする生成 | 実行時 Type 経路は素の Dictionary より遅い(1.93 倍)。ジェネリック API で受けて static 解決する | [TYP-01](../benchmarks/results/TYP-01-TypeMap.md) |

詳細は [rejected-patterns.md](rejected-patterns.md)。

---

## 🧪 生成コードの検証

- 生成コードにも本カタログの検証プロセスを適用する: **等価性テスト**(生成形 = 素直な実装の結果一致)を必ず用意する(GEN-01 の注意と同じ)
- 性能主張は測定してから記録する。計測が誤差範囲なら生成コード(JitDisasm)まで確認して「➖誤差 / 差なし」を区別する([benchmark-methodology.md](benchmark-methodology.md) の判断基準)
- JIT 環境向けに Emit の高速パスを併設する場合は `RuntimeFeature.IsDynamicCodeCompiled` で分岐する(AOTS-08)。ただし GEN-01 の実測が示すとおり、**直書き生成コードは Emit の最良形と同等**なので、二重パスが必要になる場面は「生成できない動的シナリオ」に限られる
