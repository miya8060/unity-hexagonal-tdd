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

## fork 直後の green 確認 (Step 0)

書き換え始める前に、**雛形が手元で green に動くこと** を確認する。これで「以降の red は自分の置換漏れ」と切り分けられる。

### Step 0.1: tests-net で `dotnet test` を通す

```sh
cd tests-net
dotnet restore
dotnet test
```

Domain (`Greeter`) + Application (`IClock`) + EditMode (`GreeterSpec`, `SmokeSpec`) が pass すれば OK。failure が出る場合は .NET SDK 8.0.x が入っているか (`dotnet --list-sdks` で確認)、`<TargetFramework>net8.0</TargetFramework>` と整合しているかを最初に疑う。

### Step 0.2: Unity Editor を開いて Test Runner で EditMode + PlayMode を通す

1. Unity Hub から repo を Add し、**Unity 6000.0.74f1** で開く (初回起動でドメインリロードが数十秒走る)
2. `Window → General → Test Runner` を開く
3. **EditMode** タブ: `GreeterSpec` と `SmokeSpec` が pass することを確認
4. **PlayMode** タブ: `UnityClockSpec.Now_advances_with_Time_time` が pass することを確認 (`Time.time` が 0.1 秒進むのを待って assert する `[UnityTest]`)

これで「fork 直後の green 状態」が手元で再現できた証拠が取れる。`Use this template` 経由で fork した場合、source 側の最新 green commit を継承しているはずなので、ここで red になることは原則ない。red の場合は雛形側 (`miya8060/unity-hexagonal-tdd`) の最新コミットの CI status と比較する。

### Step 0.3 (任意): MCP for Unity をセッション開始

Claude Code や他の MCP クライアントから Editor 越しにテストを呼びたい場合:

1. Unity Editor で `Window → MCP for Unity` を開く
2. **Start Session** ボタンを押す (これを押さないと session が張られず MCP 側から見えない)
3. クライアント側 (Claude Code 等) で `mcpforunity://instances` リソースを読み、`<RepoName>@<hash>` が見えることを確認
4. 複数 Unity インスタンスを開いている場合は `set_active_instance` で対象を pin する

MCP を使わない場合はこの Step 0.3 をスキップして次の Step A へ進む。

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

**ファイル名自体もリネーム** する: `UnityHexagonalTdd.Domain.asmdef` → `MyGame.Domain.asmdef` (`.asmdef.meta` も追従させる)。履歴を残すため `git mv` を使うのが推奨 (`mv` でも git の rename detection で追従するが、意図と history が明示される)。

### Step C: 既存 .cs の namespace を書き換える

雛形には 7 ファイルの .cs が入っている (production code 3 + test code 4):

| ファイル | 旧 namespace | 新 namespace |
|---|---|---|
| `Assets/Scripts/Domain/Greeter.cs` | `UnityHexagonalTdd.Domain` | `MyGame.Domain` |
| `Assets/Scripts/Application/IClock.cs` | `UnityHexagonalTdd.Application` | `MyGame.Application` |
| `Assets/Scripts/Presentation/Adapters/UnityClock.cs` | `UnityHexagonalTdd.Presentation.Adapters` | `MyGame.Presentation.Adapters` |
| `Assets/Tests/EditMode/GreeterSpec.cs` | `UnityHexagonalTdd.Tests.EditMode` | `MyGame.Tests.EditMode` |
| `Assets/Tests/EditMode/SmokeSpec.cs` | `UnityHexagonalTdd.Tests.EditMode` | `MyGame.Tests.EditMode` |
| `Assets/Tests/PlayMode/Fakes/FakeClock.cs` | `UnityHexagonalTdd.Tests.PlayMode.Fakes` | `MyGame.Tests.PlayMode.Fakes` |
| `Assets/Tests/PlayMode/UnityClockSpec.cs` | `UnityHexagonalTdd.Tests.PlayMode` | `MyGame.Tests.PlayMode` |

各ファイル内の `using UnityHexagonalTdd.<Layer>;` (= 他層を参照する using) もすべて新 namespace に書き換える。一括置換ツール (Rider / VS Code の Find & Replace in Files) で `UnityHexagonalTdd` → `MyGame` を Project ルートに掛けると漏れにくい。

### Step D: 姉弟 .csproj を書き換える

`tests-net/UnityHexagonalTdd.Tests.csproj`:

- ファイル名を `MyGame.Tests.csproj` にリネーム (`git mv` 推奨、Step B と同じ理由)
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

### Step H: dogfood seed (Greeter / UnityClockSpec) の扱い

雛形には CI green の seed として 2 つの「dogfood」要素が入っている:

- `Greeter` (Domain) + `GreeterSpec` (EditMode): engine-free TDD 経路の green seed
- `UnityClockSpec` (PlayMode): `[UnityTest]` で Unity の `Time.time` が進むことを assert する PlayMode green seed

**推奨: 残す**。理由:

1. これらが green であることが「雛形が fork 後も壊れていない」証拠になる (CI と Test Runner の両方で確認可能)
2. 最初のドメインを書く際の「ファイル配置 + namespace + asmdef references + spec の書き方」の参照実装になる
3. 不要になっても、最初のドメインが green になってから削除すれば良い (削除コストは後ろ倒し可能)

**消す場合**: `Greeter.cs` / `GreeterSpec.cs` / `UnityClockSpec.cs` を削除 (`.meta` ファイルも忘れずに)。削除後 CI で「テストが 0 件」になることに注意。**自分のドメインを 1〜2 個書き、参照実装として不要になってから削除する**のが安全。spec の書き方・asmdef references・PlayMode の `[UnityTest]` 構造で迷ったときに seed を参照できなくなるコストの方が、seed を残しておく違和感より大きいことが多い。

### Step I: README を自プロジェクト向けに書き換える

雛形の `README.md` には「unity-hexagonal-tdd 雛形を fork した状態」の説明が書かれている。fork したプロジェクトでは:

- タイトル `# unity-hexagonal-tdd` を `# <YourProject>` に変更
- 1 段落目の説明を自プロジェクトの目的に書き換え
- `## 関連プロジェクト` 節は不要になることが多いので削除 (または「この雛形は `miya8060/unity-hexagonal-tdd` から fork した」と明記)
- `## ライセンス` 節を Step G で追加した LICENSE に合わせて埋める

### Step J: rename 後の最終確認と CI green の継承

ここまでの置換が完了したら、もう一度 green を確認して push する。

1. `cd tests-net && dotnet test` がローカル green になることを確認 (= namespace 置換漏れが残っていない証拠)
   - red が出る場合: 多くは asmdef の `references` 配列か .cs の `using` / `namespace` 宣言に置換漏れがある。`grep -r UnityHexagonalTdd .` で検出できる
2. Unity Editor で Test Runner を開いて EditMode + PlayMode が green であることを確認 (PlayMode は CI で走らないのでここで必ず通す)
3. push して GitHub Actions が green になるのを待つ
4. **`Use this template` 経由で green を継承した状態 = この時点でフォーク先が自分のドメインを書き始める準備完了** という stamp が成立

CI が red になった場合は、まずローカル `dotnet test` で再現する (Unity Editor 起動なしに debug できるのが姉弟 .csproj 設計の利点)。

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

#### EditMode 用の port test double

Application のユースケースを spec する時、`IClock` 等の port にどう test double を用意するかは最初に迷うポイント。雛形に同梱の `FakeClock` は `Assets/Tests/PlayMode/Fakes/` にあり PlayMode test asmdef のみで参照可能なので、**EditMode の spec からは見えない**。

軽い解決策は spec ファイルの中に inline で stub を書くこと:

```csharp
private sealed class StubClock : IClock { public float Now { get; set; } }
```

これで EditMode 単独で deterministic な検証が成立する。参照実装: [DayCycleControllerSpec.cs](https://github.com/miya8060/unity-template-trial-daycycle/blob/main/Assets/Tests/EditMode/DayCycleControllerSpec.cs)。

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

#### MonoBehaviour に test 用注入 seam を生やすパターン

`Awake` の中で default adapter (例: `new UnityClock()`) を作る MonoBehaviour を test から差し替えたい場合、public な注入メソッドを生やすのが軽い。代表的な 2 つ:

- **`Configure(IClock, ...)` 系**: `Awake` 後に port を上書きする。test では `GameObject.AddComponent` の直後に `Configure` を呼ぶ。
  参照: [DayCycleHost.cs](https://github.com/miya8060/unity-template-trial-daycycle/blob/main/Assets/Scripts/Presentation/DayCycleHost.cs) の `Configure(IClock, float, DayPhase)`
- **`Bind(...)` 系**: `[SerializeField] private OtherMonoBehaviour other;` のような直接参照を test から差し替える。production では Bootstrap が inspector 経由で繋ぐが、test では dynamic に組んだ GameObject を `Bind` で渡す。
  参照: [SkyTint.cs](https://github.com/miya8060/unity-template-trial-daycycle/blob/main/Assets/Scripts/Presentation/SkyTint.cs) の `Bind(DayCycleHost, Camera)`

どちらも production の `[SerializeField]` 注入経路を残したまま、test だけ別経路を作るための小さな seam。constructor injection が使えない MonoBehaviour で test ability を確保する常套手段。

### Editor を開きっぱなしで MCP from CLI

Editor を開いた状態のまま Claude Code 等から PlayMode テストを呼びたい場合は MCP for Unity 経由:

- `mcp__UnityMCP__run_tests` で全 PlayMode 走らせ
- `mcp__UnityMCP__get_test_job` で結果取得
- `mcp__UnityMCP__refresh_unity` で .meta 強制再生成 (新規 .cs を作ったあと)

詳細は MCP for Unity 側のドキュメント参照。

### dotnet test と Unity Test Runner の cross-validation

姉弟 .csproj は `<Compile Include>` で Unity 側の source を取り込んでいるので、両者の結果は原理的に一致する。ただし `<Compile Include>` の glob 漏れ・asmdef の `defineConstraints` ミスマッチ・`UNITY_EDITOR` 等の define 差で「dotnet test は green だが Unity Test Runner では fail」という乖離が起きうる。

**最初の自分の spec を 1 つ書いたら、両方の runner で同じ結果になることを必ず 1 回確認する**:

- [ ] `cd tests-net && dotnet test` で新 spec が green (または期待通り red)
- [ ] Unity Editor の Test Runner で同じ spec が同じ結果

一致しない場合は Step D の `<Compile Include>` のパスか asmdef references / namespace の置換漏れが残っている可能性が高い。最初の 1 spec で気付ければ後の TDD loop で混乱しない。

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

### subfolder を切ると Unity 起動前に folder.meta が生成されない

`Assets/Scripts/Domain/DayCycle/` のように subfolder を作る場合、`DayCycle.meta` (folder の .meta) は Unity Editor を一度開かないと生成されない。先に `git add` してしまうと .meta なしの folder が commit され、後で Unity を開いた時に warning + 追加 commit が必要になる。

- 推奨: subfolder を git に追加する前に Unity Editor を一度開いて folder .meta を生成する
- もしくは: 最初は flat 構造 (`Assets/Scripts/Domain/*.cs`) で進め、ファイル数が増えてから subfolder 化

### MCP for Unity が反応しない

Editor を再起動 → `mcp__UnityMCP__refresh_unity` を `mode=force, scope=all` で実行 → `mcp__UnityMCP__get_console_logs` で接続診断。

## 関連プロジェクト

- **AtmoTest** (`miya8060/AtmoTest`): この雛形を抽出する元になった参照実装。`Mood` というドメインを 4 層 + port + adapter で実装し終えた状態が見られる。雛形に何を入れて何を入れなかったかの実証ベース
- **MCP for Unity** (`CoplayDev/unity-mcp`): 同梱パッケージ本体

## 既存 Unity プロジェクトに後から CI を載せたい場合

この雛形は **新規プロジェクトの onramp** に最適化されている。既に進行中の Unity プロジェクトに姉弟 .csproj + GitHub Actions パターンを後付けしたい場合は、AtmoTest の commit 履歴 (Step 5: PR #5 で姉弟 csproj 追加 → PR #6 で workflow 追加) を参考に、同じ手順を手作業で当てる方針が確実。
