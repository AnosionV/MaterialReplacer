# Material Replacer

## 概要
`Material Replacer` は、VRChat アバター改変者向けに設計された Unity エディタ拡張です。このツールは、`MeshRenderer` コンポーネントに適用された特定のマテリアルを検索し、Unity シーン内のゲームオブジェクトに対して一括で置き換えを行うことができます。ゲームオブジェクトはルートオブジェクト（通常はアバターですが、シーン内の任意の GameObject に対応）ごとにグループ化されます。

## 機能
- **マテリアル検索:** `MeshRenderer` コンポーネントに特定のマテリアルが適用されているゲームオブジェクトを検索します。
- **マテリアル置換:** 選択したマテリアルを、新しいマテリアルに置き換えます。
- **ルートオブジェクトのトグルコントロール:** ルートオブジェクト（例: アバターやその他の GameObject）全体に対して、マテリアル置換のオン/オフを切り替えます。
- **個別オブジェクトのトグルコントロール:** ルートオブジェクト内の各ゲームオブジェクトに対して、マテリアル置換のオン/オフを切り替えます。
- **Undo サポート:** 置換操作は Unity の Undo システムで管理されます。

## 使い方

1. `Material Replacer` ウィンドウを開く:
   - Unity エディタのメニューから `Window > Material Replacer` に移動します。

2. 検索対象のマテリアルを追加:
   - `Target Materials` フィールドに、検索したいマテリアルを追加します。複数のマテリアルを追加可能です。

3. 置き換え先のマテリアルを選択:
   - `Replace Material` フィールドで、置き換え先のマテリアルを選択します。

4. 検索結果の確認:
   - 対象マテリアルが選択されると、そのマテリアルが `MeshRenderer` コンポーネントに適用されているすべてのゲームオブジェクトが表示されます。
   - ゲームオブジェクトはルートオブジェクトごとにグループ化され、ルートオブジェクトごとに置換のオン/オフを切り替えることができます。

5. マテリアルの置換:
   - `Replace` ボタンを押して、選択したマテリアルを指定されたゲームオブジェクトに置き換えます。

## 注意事項
- このツールは、`MeshRenderer` コンポーネントに適用されたマテリアルのみを検索します。
- ルートオブジェクトはシーン内の任意の GameObject です。アバターに限らず、様々なルートオブジェクトに対応しています。
- 置換操作は Unity の Undo システムで元に戻すことができます。
