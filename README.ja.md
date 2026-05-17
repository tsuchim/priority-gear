# PriorityGear

PriorityGear は Windows 11 向けのプロセス優先度管理ツールです。通常時の優先度と、対象プロセスが前面ウィンドウを持つときの優先度をルールで切り替えます。

古い優先度管理ツールの利用体験を参考にしていますが、AutoGear のコード、資産、UI、内部挙動をコピーしないクリーンルーム実装です。

## v0.1 User Mode

- WPF GUI とタスクトレイ常駐
- ユーザー単位の JSON ルール
- 実行ファイル名・フルパス・パス末尾による一致
- 通常時優先度とアクティブ時優先度
- 前面ウィンドウ検出
- ルールの追加、編集、削除、有効/無効切り替え
- 適用結果、失敗理由、現在の希望優先度の表示
- 繰り返し失敗ログの抑制

## v0.1 で扱わないもの

Windows Service、インストーラー、署名、Microsoft Store 配布、winget、システム全体ルール、`svchost.exe` のサービス名一致、Realtime 優先度 UI、テレメトリ、ネットワーク通信、アップデーターは含めません。

User Mode は管理者権限を要求せず、現在のユーザーが制御できるプロセスだけを対象にします。System Mode は管理者承認を前提にした preview として提供しています。

ルールは `%LocalAppData%\PriorityGear\rules.json` に保存されます。メインウィンドウを閉じるとアプリは終了します。

## 注意事項

PriorityGear はプロセス優先度を変更するため、システムの応答性や安定性に影響する可能性があります。
本ソフトウェアは MIT License に基づき AS IS（現状有姿）で提供されます。
利用は自己責任です。
System Mode は管理者が明示的に有効化する高度な機能として扱います。
Windows の保護機構を迂回する機能は実装しません。
System Mode 開発では、読み取り専用 status pipe と管理者専用 mutation pipe を分離します。呼び出し元を確認できない場合、変更系コマンドは拒否します。

## ビルドと実行

```powershell
dotnet restore PriorityGear.slnx
dotnet build PriorityGear.slnx --configuration Release --no-restore
dotnet test PriorityGear.slnx --configuration Release --no-build
dotnet run --project src/PriorityGear.App/PriorityGear.App.csproj --configuration Release
```

## v0.3.2 System Mode Installer Release

`v0.3.2` は formal System Mode installer 用の現在の GitHub release です。winget submission に必要な silent installer switches を追加しています。この installer が PriorityGear の通常の GitHub 配布インストール経路になります。

公開 artifact は次です。

```text
PriorityGear-v0.3.2-win-x64-installer.zip
```

zip には `PriorityGear.Setup.exe` が含まれます。ダブルクリックして UAC を承認すると PriorityGear を install / update します。この installer は AS IS であり、署名を明示的に追加するまでは unsigned です。

同じ installer artifact をローカルで作成する場合:

```powershell
.\scripts\package-release.ps1 -TagName "v0.3.2" -OutputDirectory ".\artifacts\release-test-v0.3.2"
```

installer は GUI app を配置し、`PriorityGear.Service` を `%ProgramFiles%\PriorityGear\versions` 以下の versioned directory から起動する LocalSystem Windows Service として構成します。`%ProgramData%\PriorityGear\rules.machine.json` と `%ProgramData%\PriorityGear\Logs` は保持します。

uninstall は次で実行します。

```text
PriorityGear.Setup.exe --uninstall
```

uninstall は service を停止・削除し、installed program files を削除します。ProgramData は既定で保持します。

v0.2 検証では、対話ユーザー側の TestTarget、machine-rule monitor path、一時的な LocalSystem-owned `PriorityGear.TestTarget.Service`、targeted service discovery、その安全な temporary service への service-name machine rule が成功済みです。SCM API discovery 後、検証全体はテスト環境で約 8 秒になっています。

v0.2 System Mode line には最初の service-side machine-rule monitor が入っています。machine rule は `%ProgramData%\PriorityGear\rules.machine.json` に保存され、有効かつ管理者承認済みの rule だけが適用対象です。管理は admin pipe / CLI 経由です。SCM API ベースの service-process discovery と service-name rule も入りましたが、shared-host safety gate 付きです。shared-host `svchost.exe` の dry-run / reject は検証済みですが、任意の `svchost.exe` 制御はまだ主張しません。

v0.2 の範囲は LocalSystem service の検証用 install/update、status/admin named pipe、service 経由の priority mutation、machine-rule monitor、service-process discovery、service-name machine rule、CLI 管理、最小限の GUI System Mode status 表示です。

`v0.2.1` は System Mode status visibility 用の直前の public release ですが、artifact はまだ verification setup zip でした。

`v0.2.0-preview.1` は System Mode foundation の過去の public prerelease として残ります。

Store 配布、署名、本番 MSI/MSIX packaging、GUI machine-rule editing、System Mode の active-window priority switching、任意の shared-host mutation、CPU affinity、I/O priority、EcoQoS、Realtime priority UI、driver、telemetry、network、updater は範囲外です。winget package は `microsoft/winget-pkgs` PR が validation / merge されるまで利用可能とは扱いません。

検証後の状態として、`PriorityGear.Service` は install/running のまま残る場合があります。一時的な `PriorityGear.TestTarget.Service` と temporary machine rules は削除され、`%ProgramData%\PriorityGear\Logs` は残ります。`%ProgramData%\PriorityGear\rules.machine.json` は保持または復元されます。古い version directory cleanup は best-effort です。

ライセンスは MIT です。
