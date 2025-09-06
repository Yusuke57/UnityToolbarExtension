# UnityToolbarExtension
Customizable Unity Editor toolbar

- Unityエディタ上部に、任意のボタンやスライダー等を表示できるエディタ拡張
- 自作機能を自由に入れられるほか、デフォルト機能もいくつか用意
- ProjectSettingsから有効無効の切り替えや表示場所の変更が可能

## デフォルト機能
<img width="900" src="https://github.com/user-attachments/assets/7461eeb4-208e-45da-b08d-6811de1140a0" />


- プレイ中の音量変更: AudioListener.valueを変更する。アイコンクリックで最大音量に戻す
- Sceneのリロード: プレイ中、現在のシーンを再読み込みする
- TimeScaleの変更: Time.timeScaleを変更する。アイコンクリックで1に戻す
- Prefab表示履歴: 開いたPrefabの履歴リストを表示し、選択するとそのPrefabを開く
- Scene一覧: プロジェクト内のSceneリストを表示し、選択するとそのSceneを開く

## 設定
<img width="635" height="476" alt="image" src="https://github.com/user-attachments/assets/e82017bc-7aba-4fee-a565-2afaf44f5fa6" />

ProjectSettings の Toolbar Extension で、機能の有効無効や表示位置、並び順を変更できる

## カスタム機能
**IToolbarElement** を実装し、

- デフォルトの表示位置を返すプロパティ
- 表示するVisualElementを返すメソッド

を記述することで表示される

```cs
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using YujiAp.UnityToolbarExtension.Editor;

namespace Sample.Editor
{
    public class ToolbarExtensionSampleButton : IToolbarElement
    {
        public ToolbarElementLayoutType DefaultLayoutType => ToolbarElementLayoutType.RightSideRightAlign;

        public VisualElement CreateElement()
        {
            var button = new EditorToolbarButton(() => Debug.Log("Sample Button Clicked"));
            button.text = "Sample";
            return button;
        }
    }
}
```

<img width="320" alt="image" src="https://github.com/user-attachments/assets/95025e2d-1b54-44eb-8b8d-3f225d8454bd" />

## 導入方法
1. PackageManager左上の「+」ボタンから「Install package from git URL...」を選択
2. `https://github.com/Yusuke57/UnityToolbarExtension.git` を入力して「Install」を押下

## 確認環境
- MacOS
- Unity6.1
