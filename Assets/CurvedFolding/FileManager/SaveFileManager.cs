using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using SFB;

namespace CurvedFoldingSystem.Components.FileManager
{
    public class SaveFileManager : MonoBehaviour
    {
        [field: SerializeField]
        public CreaseCurve Crease { get; set; }
        [field: SerializeField]
        public ScalableCurve Handle {  get; set; }
        [field: SerializeField]
        public BasicCurve Developed {  get; set; }
        [field: SerializeField]
        public CreasePatternEdge Edge {  get; set; }
        [field: SerializeField]
        public CurvedFolding Folding { get; set; }

        private string previousPath;

        /*
        [RuntimeInitializeOnLoadMethod]
        static void OnRuntimeMethodLoad()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 0 && !string.IsNullOrEmpty(args[1]))
            {
                previousPath = args[1];
            }

        }

        private void Awake()
        {
            if(previousPath != null)
            {
                string crvf = File.ReadAllText(previousPath);
                SaveFileManagerData data = JsonUtility.FromJson<SaveFileManagerData>(crvf);
                data.Load(this);
                Folding.UpdateCurvedFolding();
            }
        }
        */

        [ContextMenu("Save As")]
        public bool SaveAs()
        {
            SaveFileManagerData data = new(this);
            string crvf = JsonUtility.ToJson(data);
            var path = StandaloneFileBrowser.SaveFilePanel("編集ファイルを保存", "", "untitled", "crvf");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, crvf);
                previousPath = path;
                return true;
            }
            return false;
        }

        [ContextMenu("Save")]
        public bool Save()
        {
            if (string.IsNullOrEmpty(previousPath))
                return false;

            SaveFileManagerData data = new(this);
            string crvf = JsonUtility.ToJson(data);
            File.WriteAllText(previousPath, crvf);
            return true;
        }

        [ContextMenu("Load")]
        public bool Load()
        {
            var paths = StandaloneFileBrowser.OpenFilePanel("編集ファイルを開く", "", "crvf", false);
            if (paths.Length > 0)
            {
                string crvf = File.ReadAllText(paths[0]);
                SaveFileManagerData data = JsonUtility.FromJson<SaveFileManagerData>(crvf);
                data.Load(this);
                Folding.UpdateCurvedFolding();
                return true;
            }
            return false;
        }

        [ContextMenu("Export SVG")]
        public void ExportSVG()
        {
            List<DividedCurve> curves = new List<DividedCurve>()
            {
                Folding._2D.HandleSurface.Curve.WorldDivided,
                Folding._2D.BihandleSurface.Curve.WorldDivided,
                Folding.testCurve.WorldDivided
            };
            string svg = Export.ToSVG(curves, Edge.Paper);
            var path = StandaloneFileBrowser.SaveFilePanel("展開図をエクスポート", "", "CreasePattern", "svg");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, svg);
            }
        }

        [ContextMenu("Export OBJ")]
        public void ExportOBJ()
        {
            Mesh handle = Folding._3D.HandleSurface.SurfMesh.Strip;
            Mesh Bihandle = Folding._3D.BihandleSurface.SurfMesh.Strip;
            string obj = Export.ToOBJ(new List<Mesh>() { handle, Bihandle });
            var path = StandaloneFileBrowser.SaveFilePanel("3Dモデルをエクスポート", "", "shape", "obj");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, obj);
            }
        }
    }
}
