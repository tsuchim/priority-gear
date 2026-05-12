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

User Mode は管理者権限を要求せず、現在のユーザーが制御できるプロセスだけを対象にします。System Mode は将来のマイルストーンで、管理者承認を前提に設計します。

ルールは `%LocalAppData%\PriorityGear\rules.json` に保存されます。メインウィンドウを閉じるとアプリは終了します。

## 注意事項

PriorityGear はプロセス優先度を変更するため、システムの応答性や安定性に影響する可能性があります。
本ソフトウェアは MIT License に基づき AS IS（現状有姿）で提供されます。
利用は自己責任です。
System Mode は管理者が明示的に有効化する高度な機能として扱います。
Windows の保護機構を迂回する機能は実装しません。

## ビルドと実行

```powershell
dotnet restore PriorityGear.slnx
dotnet build PriorityGear.slnx --configuration Release --no-restore
dotnet test PriorityGear.slnx --configuration Release --no-build
dotnet run --project src/PriorityGear.App/PriorityGear.App.csproj --configuration Release
```

ライセンスは MIT です。
