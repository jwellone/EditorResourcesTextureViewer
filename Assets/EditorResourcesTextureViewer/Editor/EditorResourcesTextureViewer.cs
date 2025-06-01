using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace jwelloneEditor
{
    public sealed class EditorResourcesTextureViewer : EditorWindow
    {
        const int _buttonSize = 24;

        [SerializeField] int _selectIndex;
        [SerializeField] Vector2 _scrollPosition;
        readonly List<Texture> _textures = new();

        [MenuItem("Tools/jwellone/Editor Resources Viewer")]
        static void Init()
        {
            var window = EditorWindow.GetWindow(typeof(EditorResourcesTextureViewer));
            window.Show();
        }

        void OnEnable()
        {
            var assetPaths = Resources.FindObjectsOfTypeAll(typeof(Texture))
                .Where(x => AssetDatabase.GetAssetPath(x) == "Library/unity editor resources")
                .Select(x => x.name)
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            _textures.Clear();
            foreach (var assetPath in assetPaths)
            {
                var texture = (Texture)EditorGUIUtility.Load(assetPath);
                if (texture != null)
                {
                    _textures.Add(texture);
                }
            }
        }

        void OnDisable()
        {
            _textures.Clear();
        }

        void OnGUI()
        {
            var texture = _textures[_selectIndex];
            GUILayout.Label($"[Path]{texture.name} [Size]{texture.width} x {texture.height} [Format]{texture.graphicsFormat}");

            EditorGUILayout.BeginVertical();

            GUILayout.BeginHorizontal(GUILayout.Height(position.size.y / 2f));

            DrawTexture(texture);

            GUILayout.EndHorizontal();

            DrawButtonArea();

            EditorGUILayout.EndVertical();
        }

        void DrawTexture(Texture texture)
        {
            var width = position.size.x;
            var height = position.size.y / 2f;
            var texWidth = (float)((width > height) ? height : width);
            var texHeight = texWidth * texWidth / texWidth;

            if (texture.width > texture.height)
            {
                texHeight *= texture.height / (float)texture.width;
            }
            else
            {
                texWidth *= texture.width / (float)texture.height;
            }

            var spaceWidth = (width - texWidth) / 2f;
            GUILayout.BeginHorizontal("box");
            GUILayout.Space(spaceWidth);

            var spaceHeight = (height - texHeight) / 2f;
            GUILayout.BeginVertical();
            GUILayout.Space(spaceHeight);

            var textureRect = GUILayoutUtility.GetRect(texWidth, texHeight, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

            GUI.DrawTexture(textureRect, texture);

            GUILayout.Space(spaceHeight);
            GUILayout.EndVertical();

            GUILayout.Space(spaceWidth);
            GUILayout.EndHorizontal();
        }

        void DrawButtonArea()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                var xNum = Mathf.FloorToInt(position.width / (_buttonSize + 3.5f));
                var yNum = _textures.Count / (xNum - 1);
                var index = 0;
                var btnContent = new GUIContent();
                for (var y = 0; y < yNum; ++y)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (var x = 0; x < xNum && index < _textures.Count; ++x, ++index)
                    {
                        var texture = _textures[index];
                        btnContent.image = texture;
                        btnContent.tooltip = texture.name;

                        if (GUILayout.Button(btnContent, GUILayout.Width(_buttonSize), GUILayout.Height(_buttonSize)))
                        {
                            _selectIndex = index;
                            GUIUtility.systemCopyBuffer = texture.name;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }
    }
}