# template-usage

`unity-hexagonal-tdd` 雛形を fork してから自分のドメインを書き始めるまでの具体手順。README は概要、こちらは「**実際に何を触れば動くか**」を扱う。

## この template の前提

### 1. ports & adapters (hexagonal) を 4 層 asmdef で表現する

| 層 | asmdef 名 | `noEngineReferences` | references | 役割 |
|---|---|---|---|---|
| Domain | `UnityHexagonalTdd.Domain` | `true` | `[]` | エンティティ・値オブジェクト・ドメインルール (POCO のみ) |
| Application | `UnityHexagonalTdd.Application` | `true` | `[Domain]` | port (interface) とユースケース (POCO のみ) |
| Presentation | `UnityHexagonalTdd.Presentation` | `false` | `[Domain, Application]` | MonoBehaviour と adapter (Unity Engine 触る側) |
| Bootstrap | `UnityHexagonalTdd.Bootstrap` | `false` | `[Domain, Application, Presentation]` | composition root (DI 配線) |

**重要なのは Domain と Application が engine-free であること**。これが `dotnet test` で走らせる前提を成立させており、CI が軽く回る理由でもある。Unity Engine API (`UnityEngine.*`) を Domain / Application に持ち込んだ瞬間、姉弟 .csproj のコンパイルが落ちる (= 雛形の利点が消える)。

### 2. tests-net/ 姉弟 .csproj で Unity を起動せずテストする

`tests-net/UnityHexagonalTdd.Tests.csproj` は Unity の asmdef とは独立した **通常の .NET プロジェクト** で、`<Compile Include="../Assets/Scripts/Domain/**/*.cs">` のように Unity 側のソースを参照する。Unity Editor を一切起動せず、`dotnet test` だけで Domain + Application + EditMode テストが走る。

これにより:

- ローカルで Unity 起動を待たず TDD を回せる (Editor のドメインリロード = 数秒〜数十秒のロスが消える)
- GitHub Actions の無料枠だけで CI が組める (Unity license activation 不要)
- Rider / VS Code から `.csproj` を直接開いて NUnit テストランナーが使える

trade-off は **PlayMode テストが CI で走らない** こと。PlayMode は MonoBehaviour ライフサイクルや GameObject 統合に依存するため、Unity Editor (= Test Runner) でしか実行できない。これは意図した割り切り。

### 3. EditMode と PlayMode を明確に分ける

| テスト種別 | 対象 | 走る場所 |
|---|---|---|
| EditMode (`Assets/Tests/EditMode/`) | Domain + Application の純ロジック | `dotnet test` (CI) + Unity Test Runner |
| PlayMode (`Assets/Tests/PlayMode/`) | MonoBehaviour + GameObject 統合 | Unity Test Runner のみ |

「ユースケース 1 つ書いたら EditMode に spec を 1 つ」「MonoBehaviour に挙動を足したら PlayMode に spec を 1 つ」という対応関係を保つと、CI で守れる範囲とローカル限定で守る範囲が綺麗に分かれる。

### 4. MCP for Unity が同梱

`Packages/manifest.json` に `com.coplaydev.unity-mcp` が入っている。Claude Code や他の MCP クライアントから `run_tests` / `refresh_unity` を呼んで Editor 越しに PlayMode テストを実行できる。**不要なら削除して構わない** (削除手順は後述)。

## 最初に何を書き換えるか

`Use this template` 直後にやる作業。順番通りにやれば 10〜20 分で自分のプロジェクトに化ける。

### Step A: プロジェクト名を決める

例として `MyGame` (namespace `MyGame`) に置き換えるとする。

### Step B: asmdef の `name` と `rootNamespace` を 6 ファイル書き換える

```
Assets/Scripts/Domain/UnityHexagonalTdd.Domain.asmdef
Assets/Scripts/Application/UnityHexagonalTdd.Application.asmdef
Assets/Scripts/Presentation/UnityHexagonalTdd.Presentation.asmdef
Assets/Scripts/Bootstrap/UnityHexagonalTdd.Bootstrap.asmdef
Assets/Tests/EditMode/UnityHexagonalTdd.Tests.EditMode.asmdef
Assets/Tests/PlayMode/UnityHexagonalTdd.Tests.PlayMode.asmdef
```

各ファイルで:

- `"name": "UnityHexagonalTdd.<Layer>"` → `"name": "MyGame.<Layer>"`
- `"rootNamespace": "UnityHexagonalTdd.<Layer>"` → `"rootNamespace": "MyGame.<Layer>"`
- `"references"` 配列内の `"UnityHexagonalTdd.<Layer>"` を `"MyGame.<Layer>"` に置換

**ファイル名自体もリネーム** する: `UnityHexagonalTdd.Domain.asmdef` → `MyGame.Domain.asmdef` (`.asmdef.meta` も追従させる)。

### Step C: 既存 .cs の namespace を書き換える

雛形には 4 ファイルだけ .cs が入っている:

| ファイル | 旧 namespace | 新 namespace |
|---|---|---|
| `Assets/Scripts/Application/IClock.cs` | `UnityHexagonalTdd.Application` | `MyGame.Application` |
| `Assets/Scripts/Presentation/Adapters/UnityClock.cs` | `UnityHexagonalTdd.Presentation.Adapters` | `MyGame.Presentation.Adapters` |
| `Assets/Tests/EditMode/SmokeSpec.cs` | `UnityHexagonalTdd.Tests.EditMode` | `MyGame.Tests.EditMode` |
| `Assets/Tests/PlayMode/Fakes/FakeClock.cs` | `UnityHexagonalTdd.Tests.PlayMode.Fakes` | `MyGame.Tests.PlayMode.Fakes` |

`UnityClock.cs` と `FakeClock.cs` は `using UnityHexagonalTdd.Application;` も書き換えること。

### Step D: 姉弟 .csproj を書き換える

`tests-net/UnityHexagonalTdd.Tests.csproj`:

- ファイル名を `MyGame.Tests.csproj` にリネーム
- `<RootNamespace>UnityHexagonalTdd</RootNamespace>` → `<RootNamespace>MyGame</RootNamespace>`
- `<Compile Include="../Assets/Scripts/Domain/**/*.cs">` などの **相対パスはそのままで OK** (Domain / Application / EditMode のディレクトリ構造を変えない限り)

リネーム後 `cd tests-net && dotnet restore && dotnet test` で green になれば書き換え成功。

### Step E: ProjectSettings の Product Name / Company Name を変える

Unity Editor で `Edit → Project Settings → Player`:

- `Company Name`
- `Product Name`

これは `ProjectSettings/ProjectSettings.asset` に反映される。

### Step F (任意): MCP for Unity を外す

MCP 経由で Editor を操作する予定がなければ、`Packages/manifest.json` から以下の行を削除:

```json
"com.coplaydev.unity-mcp": "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main",
```

削除後、Unity を再起動すると `Packages/packages-lock.json` も更新される。

### Step G (任意): LICENSE を追加する

雛形には LICENSE が入っていない。public repo にする場合は MIT 等を `LICENSE` ファイルとして追加すること。

## TDD ループの回し方

### Domain / Application を書く場合 (推奨ループ)

1. **EditMode に spec を書く** (`Assets/Tests/EditMode/<Feature>Spec.cs`)
2. `tests-net/` で `dotnet test` → red を確認
3. `Assets/Scripts/Domain/` または `Assets/Scripts/Application/` に最小実装
4. `dotnet test` → green を確認
5. refactor して再度 `dotnet test` → 引き続き green

```sh
cd tests-net
dotnet test --logger "console;verbosity=normal"
```

ファイル変更を検知して自動再実行したい場合:

```sh
dotnet watch test
```

**Unity Editor を開く必要はない**。これがこの雛形の速度的アドバンテージ。

### Presentation / Bootstrap を書く場合

MonoBehaviour や GameObject の挙動は `dotnet test` で検証できないので、PlayMode テストを使う。

1. **PlayMode に spec を書く** (`Assets/Tests/PlayMode/<Feature>Spec.cs`)
2. Unity Editor の Test Runner (Window → General → Test Runner) で PlayMode タブを選び実行 → red を確認
3. `Assets/Scripts/Presentation/` または `Assets/Scripts/Bootstrap/` に実装
4. Test Runner で再実行 → green
5. refactor

PlayMode テスト書く際の常套手段:

- Application の port (interface) を Presentation 側で adapter 実装する形に保つ
- PlayMode テストでは port に **fake を注入** して deterministic にする (例: `FakeClock` で `Time.time` を制御)
- Scene 構築は `[UnitySetUp]` 内で動的に組む (シーンファイルを git に入れない)

### Editor を開きっぱなしで MCP from CLI

Editor を開いた状態のまま Claude Code 等から PlayMode テストを呼びたい場合は MCP for Unity 経由:

- `mcp__UnityMCP__run_tests` で全 PlayMode 走らせ
- `mcp__UnityMCP__get_test_job` で結果取得
- `mcp__UnityMCP__refresh_unity` で .meta 強制再生成 (新規 .cs を作ったあと)

詳細は MCP for Unity 側のドキュメント参照。

### red → green → refactor の粒度

- 1 spec = 1 振る舞い (1 メソッドに対して複数の振る舞いがあれば spec を分ける)
- red のまま commit しない (commit するなら `[skip ci]` 含める or branch を分ける)
- green になったら **refactor を必ず挟む** (production code + test code の両方)
- PR は Step 単位 (1 ドメイン分 / 1 機能分) にまとめて squash merge

## CI 構成

`.github/workflows/test.yml` は以下を行う:

1. `actions/checkout@v4` で repo 取得
2. `actions/setup-dotnet@v4` で .NET 8 SDK 導入
3. `tests-net/` で `dotnet restore` → `dotnet test`
4. 結果 (`*.trx`) を `artifacts/` 配下に書き出し、`actions/upload-artifact@v4` でアップロード

トリガーは `push to main` / `pull_request to main` / `workflow_dispatch`。`concurrency` で同 ref の古い run はキャンセルされる。

PlayMode テストは CI では走らない (Unity Editor が要るため)。これを CI で走らせたい場合は GameCI (Unity license activation) などの別経路が必要だが、本雛形のスコープ外。

## よくあるハマりどころ

### dotnet test は通るが Unity Editor でコンパイルエラー

`<Compile Include>` のパスは正しいが Unity 側で asmdef の references / namespace が古いまま、というケースが多い。Unity Editor で Project ウィンドウを右クリック → Reimport All するか、`Library/` を消して再生成。

### EditMode テストが Test Runner に出てこない

asmdef の `defineConstraints` に `UNITY_INCLUDE_TESTS` が入っていることを確認。`autoReferenced: false` + `overrideReferences: true` + `precompiledReferences: ["nunit.framework.dll"]` の 3 点セットも必要。

### tests-net/bin と tests-net/obj が git に入りそうになる

`.gitignore` に `tests-net/bin/` と `tests-net/obj/` が入っていれば OK。Unity の `.gitignore` の `[Bb]uild/` パターンと衝突しないか念のため確認。

### MCP for Unity が反応しない

Editor を再起動 → `mcp__UnityMCP__refresh_unity` を `mode=force, scope=all` で実行 → `mcp__UnityMCP__get_console_logs` で接続診断。

## 関連プロジェクト

- **AtmoTest** (`miya8060/AtmoTest`): この雛形を抽出する元になった参照実装。`Mood` というドメインを 4 層 + port + adapter で実装し終えた状態が見られる。雛形に何を入れて何を入れなかったかの実証ベース
- **MCP for Unity** (`CoplayDev/unity-mcp`): 同梱パッケージ本体

## 既存 Unity プロジェクトに後から CI を載せたい場合

この雛形は **新規プロジェクトの onramp** に最適化されている。既に進行中の Unity プロジェクトに姉弟 .csproj + GitHub Actions パターンを後付けしたい場合は、AtmoTest の commit 履歴 (Step 5: PR #5 で姉弟 csproj 追加 → PR #6 で workflow 追加) を参考に、同じ手順を手作業で当てる方針が確実。
