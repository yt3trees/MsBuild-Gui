# MsBuild-Gui

MsBuild-GuiはMsBuildをGuiで操作・実行できるツールです。

![msbuildgui_demo gif](https://user-images.githubusercontent.com/57471763/175777936-d283d66e-9b5b-4d3f-b220-3dc5b42406b4.gif)

## Description

プロジェクト設定ボタンからDLL出力先のフォルダや使用するMsBuild.exeを登録します。

メイン画面で、登録したプロジェクトを選択するとcsproj一覧にビルド可能なcsprojの一覧がリストボックスで表示されます。

それらをダブルクリックすることでビルド対象にすることができます。その後、ビルドボタンをクリックしてビルド実行できます。

## Features

- 複数の.csprojを一括で実行可能
- Msbuildのパラメータを画面上から設定(全パラメータには未対応)

## Requirement
- Windows10
- Visual Studio 2022
- [.NET6](https://dotnet.microsoft.com/ja-jp/download/dotnet/6.0)
- [ModernWpf](https://github.com/Kinnara/ModernWpf)
- [Microsoft Visual Studio Installer Projects](https://marketplace.visualstudio.com/items?itemName=VisualStudioClient.MicrosoftVisualStudio2017InstallerProjects)

## Author

[@yt3trees](https://twitter.com/yt3trees)

## License

[MIT](https://github.com/yt3trees/MsBuild-Gui/blob/master/LICENSE)
