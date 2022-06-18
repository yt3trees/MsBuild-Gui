<div align="center">
<img src="/!image/MainWindow.png" width="700">
</div>
  
# MsBuild-Gui

MsBuild-GuiはMsBuildをGuiで操作・実行できるツールです。


## Description

プロジェクト設定ボタンからDLL出力先のフォルダや使用するMsBuild.exeを登録します。

メイン画面で、登録したプロジェクトを選択するとcsproj一覧にビルド可能なcsprojの一覧がリストボックスで表示されます。

それらをダブルクリックすることでビルド対象にすることができます。その後、ビルドボタンをクリックしてビルド実行できます。

<details>
<summary>demo</summary>

![msbuildgui_demo](https://user-images.githubusercontent.com/57471763/174423060-8f051abf-69ef-49e7-8588-8fdf33816c93.gif)

</details>
  
## Features

- 複数の.csprojを一括で実行可能
- Msbuildのパラメータを画面上から設定(全パラメータには未対応)

## Requirement
- Windows10
- Visual Studio 2022
- .NET6
- [ModernWpf](https://github.com/Kinnara/ModernWpf)
- [Microsoft Visual Studio Installer Projects](https://marketplace.visualstudio.com/items?itemName=VisualStudioClient.MicrosoftVisualStudio2017InstallerProjects)


## Author

[@yt3trees](https://twitter.com/yt3trees)

## License

[MIT](https://github.com/yt3trees/MsBuild-Gui/blob/master/LICENSE)
