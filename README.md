# unity-hexagonal-tdd

Unity 個人プロジェクトを **TDD で回しつつ CI を green にし続ける** ための雛形。Unity 6.0 URP 2D を起点に、4 層 (Domain / Application / Presentation / Bootstrap) の asmdef + 姉弟 .csproj による `dotnet test` + GitHub Actions ワークフローまでを **空のまま動く状態** で配ってある。`Use this template` でフォークし、Domain / Application に自分のドメインを書き始めれば、最初のコミットから CI が回る。

## この雛形が提供するもの

- **4 層 asmdef** (`UnityHexagonalTdd.{Domain,Application,Presentation,Bootstrap}`)
  - Domain / Application は `noEngineReferences: true` の **engine-free POCO** (Unity を起動せず `dotnet test` で走る)
  - Presentation / Bootstrap は Unity Engine に依存し、port を実装する adapter と composition root を担当
- **port + adapter のサンプル** (`IClock` interface + `UnityClock` adapter + `FakeClock` test double) が 1 組だけ入っており、新規ドメインを書く際の参照実装として使える
- **EditMode + PlayMode 二系統テスト** の asmdef が用意済み
- **tests-net/ 姉弟 .csproj**: `Assets/Scripts/Domain/**` + `Assets/Scripts/Application/**` + `Assets/Tests/EditMode/**` を `<Compile Include><Link>` でミラーし、Unity を起動せず `dotnet test` で Domain/Application の TDD を回せる
- **GitHub Actions ワークフロー** (`.github/workflows/test.yml`): push / PR で `dotnet test` が走り、結果を artifact に上げる
- **MCP for Unity** (`com.coplaydev.unity-mcp`) を `Packages/manifest.json` に同梱 (Editor 越しのテスト実行を Claude Code 等から呼ぶ用、不要なら削除可)

## 前提環境

| 項目 | バージョン / 値 |
|---|---|
| Unity Editor | 6000.0.74f1 (Unity 6.0 LTS) |
| Render Pipeline | URP (2D テンプレ) |
| .NET SDK | 8.0.x (`tests-net/` は `<TargetFramework>net8.0</TargetFramework>`) |
| テストフレームワーク | NUnit 3.14 (Unity Test Framework 1.6 互換) |
| 推奨 IDE | Rider / VS Code (どちらも `.csproj` を直接開ける) |

Unity のバージョンを変える場合は、`ProjectSettings/ProjectVersion.txt` と `Packages/manifest.json` を合わせて差し替える。

## クイックスタート

1. GitHub の `Use this template` ボタンから新しい repo を作る (or `gh repo create --template miya8060/unity-hexagonal-tdd ...`)
2. Unity Hub で clone した repo を Add し、Unity 6.0.74f1 で開く (初回起動でドメインリロードが走る)
3. **プロジェクト名を書き換える** (詳細は [`docs/template-usage.md`](docs/template-usage.md#最初に何を書き換えるか))
4. `tests-net/` で `dotnet test` を実行して green になることを確認
5. Domain にエンティティを 1 つ書き、EditMode に対応する spec を書く (red → green → refactor)

エンジンを開かずローカルで TDD を回したい場合:

```sh
cd tests-net
dotnet restore
dotnet test
```

## ディレクトリ構造

```
.
├── Assets/
│   ├── Scripts/
│   │   ├── Domain/                  # POCO のみ (no Engine ref)
│   │   ├── Application/             # ports (interface) + ユースケース (no Engine ref)
│   │   │   └── IClock.cs            # サンプル port
│   │   ├── Presentation/            # MonoBehaviour + adapter (Unity Engine 触る側)
│   │   │   └── Adapters/UnityClock.cs   # サンプル adapter
│   │   └── Bootstrap/               # composition root
│   └── Tests/
│       ├── EditMode/                # Domain + Application の純ロジックテスト
│       │   └── SmokeSpec.cs         # CI green seed
│       └── PlayMode/                # MonoBehaviour + GameObject 統合テスト
│           └── Fakes/FakeClock.cs   # サンプル test double
├── tests-net/
│   └── UnityHexagonalTdd.Tests.csproj   # 姉弟 .csproj (Domain + Application + EditMode をミラー)
├── Packages/
│   └── manifest.json                # MCP for Unity 含む
├── .github/workflows/test.yml       # dotnet test on push/PR
└── docs/
    └── template-usage.md            # 雛形の使い方詳細
```

## テストの走らせ方

| やりたいこと | 手段 |
|---|---|
| Domain / Application の TDD を Unity 起動せず回す | `tests-net/` で `dotnet test` |
| Presentation / Bootstrap の MonoBehaviour 統合テスト | Unity Editor の Test Runner (Window → General → Test Runner) で PlayMode 実行 |
| Editor を開きっぱなしで Claude Code 等から実行 | MCP for Unity 経由 (`mcp__UnityMCP__run_tests`) |
| CI で自動実行 | push / PR で `.github/workflows/test.yml` が `dotnet test` を走らせる |

PlayMode テストは Unity Engine に依存するため `dotnet test` では走らない (= CI でも走らない)。CI で検証できるのは Domain + Application のみという trade-off は意図的。詳細は [`docs/template-usage.md`](docs/template-usage.md#tdd-ループの回し方) 参照。

## ドキュメント

- [`docs/template-usage.md`](docs/template-usage.md): 雛形の前提・最初に書き換えるもの・TDD ループの具体手順

## ライセンス

(未設定。利用前に LICENSE ファイルを追加すること)
