using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace jwelloneEditor
{
    public sealed class EditorResourcesTextureViewer : EditorWindow
    {
        const int _buttonSize = 24;

        [SerializeField] int _selectIndex;
        [SerializeField] Vector2 _scrollPosition;
        readonly List<Texture2D> _textures = new();

        [MenuItem("Tools/jwellone/Editor Resources Viewer")]
        static void Init()
        {
            var window = EditorWindow.GetWindow(typeof(EditorResourcesTextureViewer));
            window.Show();
        }

        void OnEnable()
        {
            var assets = Resources.FindObjectsOfTypeAll(typeof(Texture2D))
                .Where(x => AssetDatabase.GetAssetPath(x) == "Library/unity editor resources")
                .Select(x => x.name)
                .Distinct()
                .OrderBy(x => x)
                .Select(x => EditorGUIUtility.Load(x) as Texture2D)
                .Where(x => x)
                .ToArray();

            _textures.Clear();
            _textures.AddRange(assets);
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

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Export", GUILayout.Width(48)))
            {
                var filePath = EditorUtility.SaveFilePanel("Unity Editor Resources", Application.dataPath, $"{texture.name}.png", ".png");
                Export(texture, filePath);
            }
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

        void Export(Texture2D source, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var texture = new Texture2D(source.width, source.height, source.format, source.mipmapCount > 1);
            Graphics.CopyTexture(source, texture);
            var bytes = default(byte[]);
            switch (Path.GetExtension(path))
            {
                case ".jpg":
                case ".jpeg":
                    bytes = texture.EncodeToJPG();
                    break;

                case ".tga":
                    bytes = texture.EncodeToTGA();
                    break;

                default:
                    bytes = texture.EncodeToPNG();
                    break;
            }

            DestroyImmediate(texture);
            File.WriteAllBytes(path, bytes);

            if (path.StartsWith(Application.dataPath))
            {
                AssetDatabase.ImportAsset(path.Replace(Application.dataPath, "Assets"));
            }
        }
    }
}