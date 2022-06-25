[日本語版 README](./README-ja.md)

# MsBuild-Gui

MsBuild-Gui is a tool to operate and execute MsBuild with Gui.

![msbuildgui_demo gif](https://user-images.githubusercontent.com/57471763/175777936-d283d66e-9b5b-4d3f-b220-3dc5b42406b4.gif)

## Description

Register the DLL output destination folder and MsBuild.exe to be used from the Project Settings button.

On the main screen, select the registered project and a list of buildable csproj will appear in the csproj list in a list box.

Double-click on them to make them buildable. Then click the Build button to execute the build.
  
## Features

- Multiple .csproj can be executed at once
- Msbuild parameters can be set from the screen (all parameters are not yet supported)

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
