using CurvedFoldingSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

public class UntwistMethod
{
    

    public class SubCurve
    {
        public DividedCurve crease;
        public int start;
        public int count;

        public DividedCurve handle;
        public DividedCurve subhandle;

        public SubCurve(DividedCurve curve, int from, int to)
        {
            start = from;
            count = to - from;
            crease = DividedCurve.SubCurve(curve, from, to);
        }

        public void Set3Curve(DividedCurve crease, DividedCurve handle, DividedCurve subhandle)
        {
            this.crease = crease;
            this.handle = handle;
            this.subhandle = subhandle;
        }

        public void CutEdge()
        {
            int n = crease.Count - 1;
            crease.Count = n;
            handle.Count = n;
            subhandle.Count = n;
        }

        public static List<SubCurve> SplitCurveAtInflectionPoint(DividedCurve crease, DividedCurve handle)
        {
            DevelopableSurface surf = new DevelopableSurface(crease, handle);
            List<SubCurve> curves = new List<SubCurve>();
            float prevSign = Mathf.Sign(surf.Alpha[0].x);
            int start = 0;
            for (int i = 1; i < crease.Count; i++)
            {
                float currentSign = Mathf.Sign(surf.Alpha[i].x);
                if (prevSign != currentSign)
                {
                    SubCurve sc = new SubCurve(surf.Curve, start, i - 1);
                    curves.Add(sc);
                    start = i - 1;
                }
                prevSign = currentSign;
            }
            curves.Add(new SubCurve(surf.Curve, start, crease.Count - 1));
            return curves;
        }
    }

    public class CrossSection
    {
        public Vector3 crease;
        public Vector3 handle;
        public Vector3 subhandle;
        public FrenetFrame frame;
        public float sign;

        public CrossSection(Vector3 crease, FrenetFrame frame, Vector3 handle, float curvature, float torsion, float foldRulingLength)
        {
            this.crease = crease;
            this.handle = handle;
            Vector3 r = handle - crease;
            Vector3 fr = frame.GetFoldRuling(r, curvature, torsion) * foldRulingLength;
            subhandle = crease + fr;
            this.frame = frame;

            Vector2 alpha = frame.GetAlphaSinCos(r);
            sign = Mathf.Sign(alpha.x);
        }

        public void SetNextSection(Vector3 crease, Vector3 handle, Vector3 subhandle, Vector3 tangent)
        {
            this.crease = crease;
            this.handle = handle;
            this.subhandle = subhandle;
            Vector3 r = (handle - crease).normalized;
            Vector3 fr = (subhandle - crease).normalized;
            Vector3 binormal = (r + fr).normalized;
            sign *= -1;
            frame = FrenetFrame.OrthoFrame(tangent, sign * binormal);
        }
    }

    public static float TOLERANCE = 1.0E-02f;



    public void UntwinstSubCurve(SubCurve subCurve, CrossSection crossSection, float handleScale, bool endPointIsInflection)
    {
        Debug.Log("subhandle Pos:" + crossSection.subhandle);
        Debug.Log("Untwist Start:Count:End\t" + subCurve.start + ":" + subCurve.count + ":" + (subCurve.start + subCurve.count));
        //折り目とハンドル曲線を作成
        DividedCurve crease = DividedCurve.AllignCurve(subCurve.crease, crossSection.crease, crossSection.frame);
        DividedCurve handle = DividedCurve.AllignCurve(DividedCurve.ScalingCurve(subCurve.crease, handleScale), crossSection.handle, crossSection.frame);

        //初期の情報を保存しておく
        DividedCurve initialCurve = subCurve.crease;
        float[] initialTorsions = subCurve.crease.Torsions.ToArray();


        string torsionRatioLog = "torsion ratio\n";
        List<Vector3> subHandlePositions = new List<Vector3>();
        Quaternion sumRotation = Quaternion.identity;
        int iterationCount = 0;
        for (; iterationCount < 50; iterationCount++)
        {
            string torsionLog = "iteration:" + iterationCount + " torsions\n";
            for(int i = 0; i < crease.Count; i++)
            {
                torsionLog += crease.Torsions[i] + "\n";
            }
            Debug.Log(torsionLog);

            //従ハンドル曲線が通過するべきPQ Stripを作成
            DevelopableSurface surf = new DevelopableSurface(crease, handle);
            Plane[] planes = new Plane[crease.Count];
            for(int i = 0; i < crease.Count; i++)
            {
                Vector3 fr = surf.GetFoldRuling(i);
                var frame = crease.Frames[i];
                Vector3 normal = Vector3.Cross(fr, frame.tangent).normalized;
                planes[i] = new Plane(normal, crease.Positions[i]);
            }

            //従ハンドル曲線を作成
            subHandlePositions = new List<Vector3>() { crossSection.subhandle };
            for (int i = 1; i < crease.Count; i++)
            {

                //端点について、変曲点である場合は捩率が0となるため、ハンドル曲線側のrulingと角度betaが平行になるはず。
                //よって角度betaを保存するような特別な平面を作成する。
                if(i == crease.Count - 1 && endPointIsInflection)
                {
                    Vector3 axis = Vector3.Cross(crease.Frames[i].tangent, subHandlePositions[i - 1] - crease.Positions[i]);
                    float beta = Mathf.Atan2(surf.Beta[i].y, -surf.Beta[i].x);
                    Quaternion rot = Quaternion.AngleAxis(beta * Mathf.Rad2Deg, axis);
                    planes[i] = new Plane(rot * crease.Frames[i].tangent, crease.Positions[i]);
                    Debug.Log("inflection point. plane normal:" + rot * crease.Frames[i].tangent);
                }

                Vector3 dx = crease.Positions[i] - crease.Positions[i - 1];
                Ray tangent = new Ray(subHandlePositions[i - 1], dx);
                //平面との交点が発見できるか
                if (planes[i].Raycast(tangent, out float lng) && lng >= 0)
                {
                    subHandlePositions.Add(tangent.GetPoint(lng));
                }
                //できない場合
                else
                {
                    //cotBetaの符号によって手前か奥かどちらを基準に修正するか決める
                    //捩じれによる回転方向が負（i番目が原因で交差が起きているとみなす）
                    if (Mathf.Sign(crease.Torsions[i]) * Mathf.Sign(surf.Alpha[i].y) < 0)
                    {
                        Vector3 tempPos = tangent.GetPoint(0f);
                        subHandlePositions.Add(tempPos);
                    }
                    //回転方向が正（i - 1番目が原因）
                    else
                    {
                        Vector3 tempPos = subHandlePositions[0];
                        int j = i - 1;
                        for (; j > 0; j--)
                        {
                            dx = crease.Positions[j] - crease.Positions[j - 1];
                            tangent = new Ray(subHandlePositions[j - 1], dx);
                            if (planes[i].Raycast(tangent, out lng) && lng >= 0)
                            {
                                tempPos = tangent.GetPoint(lng);
                                break;
                            }
                        }
                        for (; j < subHandlePositions.Count; j++)
                        {
                            subHandlePositions[j] = tempPos;
                        }
                        subHandlePositions.Add(tempPos);
                    }
                }
            }

            //従ハンドル曲線の検証
            DevelopableSurface subSurf = new DevelopableSurface(crease, DevelopableSurface.GetRulings(crease.Positions, subHandlePositions));
            float torsionRatio = 1.0f;
            bool shouldUpdate = false;
            for(int i = 0; i < crease.Count; i++)
            {
                float gapError = CreaseGapError(surf, subSurf, i);
                if (gapError > TOLERANCE)
                {
                    shouldUpdate = true;
                    Debug.Log(i + " has Gap. " + gapError);
                    float ratio = Mathf.Abs(SuitableTorsion(surf, subSurf, i) / initialTorsions[i]);
                    if(ratio < torsionRatio)
                    {
                        torsionRatio = ratio;
                    }
                }
            }

            //更新
            if (shouldUpdate)
            {
                //捩率を更新
                List<float> torsions = new List<float>();
                for (int i = 0; i < crease.Torsions.Count; i++)
                {
                    float tau = initialTorsions[i] * torsionRatio;
                    torsions.Add(tau);
                }

                Debug.Log("next torion ratio:" + torsionRatio);
                torsionRatioLog += torsionRatio + "\n";
                //折り目とハンドル曲線を更新
                DividedCurve untwist = UntwistCurve(initialCurve, torsionRatio, out sumRotation);
                crease = DividedCurve.AllignCurve(untwist, crossSection.crease, crossSection.frame);
                handle = DividedCurve.AllignCurve(DividedCurve.ScalingCurve(crease, handleScale), crossSection.handle, crossSection.frame);
            }
            else
            {
                Debug.Log("End loop. Count:" + iterationCount);
                Debug.Log(torsionRatioLog);
                subCurve.Set3Curve(crease, handle, new DividedCurve(subHandlePositions));
                crossSection.SetNextSection(crease.Positions[^1], handle.Positions[^1], subHandlePositions[^1], crease.Frames[^1].tangent);
                return;
            }
        }
        Debug.Log("Not reached.");
        Debug.Log(torsionRatioLog);

        for(int i = subHandlePositions.Count; i < crease.Count; i++)
        {
            subHandlePositions.Add(subHandlePositions[^1]);
        }
        DividedCurve subhandle = new DividedCurve(subHandlePositions);
        subCurve.Set3Curve(crease, handle, subhandle);
        crossSection.SetNextSection(crease.Positions[^1], handle.Positions[^1], subHandlePositions[^1], crease.Frames[^1].tangent);
        return;
    }

    public static float CreaseGapError(DevelopableSurface surf, DevelopableSurface subSurf, int i)
    {
        FrenetFrame frame = surf.Curve.Frames[i];
        float cosAlpha = surf.Alpha[i].x;
        float subCosAlpha = subSurf.Alpha[i].x;
        float diffCosAlpha = cosAlpha + subCosAlpha;
        float diffTangentPerSection = surf.Curve.Curvatures[i] * diffCosAlpha * surf.Curve.ArcLengths[i];
        float diffTangentAll = diffTangentPerSection * surf.Curve.Count;
        return Mathf.Abs(diffTangentAll);
    }

    public static float SuitableTorsion(DevelopableSurface surf, DevelopableSurface subSurf, int i)
    {
        float cotB = surf.Beta[i].x / surf.Beta[i].y;
        float subCotB = subSurf.Beta[i].x / subSurf.Beta[i].y;
        float suitableTorsion = (cotB * surf.Alpha[i].y + subCotB * subSurf.Alpha[i].y) * surf.Curve.Curvatures[i] / 2.0f;
        return suitableTorsion;
    }

    public static DividedCurve UntwistCurve(DividedCurve curve, float torsionRatio, out Quaternion sumRotation)
    {
        DividedCurve twistCurve = new DividedCurve(curve);
        for(int i = 0; i < twistCurve.Count; i++)
        {
            twistCurve.Torsions[i] *= torsionRatio;
        }
        sumRotation = Quaternion.identity;
        for(int i = 0; i < twistCurve.Count - 1; i++)
        {
            FrenetFrame frame = twistCurve.Frames[i];
            FrenetFrame nextFrame = twistCurve.Frames[i + 1];
            Quaternion currentRotReverse = Quaternion.FromToRotation(nextFrame.binormal, frame.binormal);
            Quaternion rot = Quaternion.Lerp(currentRotReverse, Quaternion.identity, torsionRatio);
            for(int j = i + 1; j < twistCurve.Count; j++)
            {
                Vector3 v = rot * (twistCurve.Positions[j] - twistCurve.Positions[i]);
                twistCurve.Positions[j] = twistCurve.Positions[i] + v;
                twistCurve.Frames[j] = rot * twistCurve.Frames[j];
            }
            sumRotation *= rot;
        }
        return twistCurve;
    }

    public UntwistMethod(DividedCurve crease, Matrix4x4 creaseToHandleCurve, float length)
    {
        DividedCurve handle = creaseToHandleCurve * crease;
        List<SubCurve> subCurves = SubCurve.SplitCurveAtInflectionPoint(crease, handle);
        Debug.Log("creaseDivPoint:" + crease.Count);
        Debug.Log("subCurvesCount:" + subCurves.Count);
        List<DividedCurve> creases = new List<DividedCurve>();
        List<DividedCurve> handles = new List<DividedCurve>();
        List<DividedCurve> subhandles = new List<DividedCurve>();

        float handleScale = creaseToHandleCurve.lossyScale.x;
        CrossSection crossSection = new CrossSection(crease.Positions[0], crease.Frames[0], handle.Positions[0], crease.Curvatures[0], crease.Torsions[0], length);
        for (int i = 0;i < subCurves.Count - 1;i++)
        {
            SubCurve sc = subCurves[i];
            UntwinstSubCurve(sc, crossSection, handleScale, true);
            sc.CutEdge();
            creases.Add(sc.crease);
            handles.Add(sc.handle);
            subhandles.Add(sc.subhandle);
        }
        UntwinstSubCurve(subCurves[^1], crossSection, handleScale, false);
        creases.Add(subCurves[^1].crease);
        handles.Add(subCurves[^1].handle);
        subhandles.Add(subCurves[^1].subhandle);

        this.crease = creases[0];
        this.handle = handles[0];
        this.subhandle = subhandles[0];
        for (int i = 1; i < creases.Count; i++)
        {
            this.crease = DividedCurve.MargeCurve(this.crease, creases[i]);
            this.handle = DividedCurve.MargeCurve(this.handle, handles[i]);
            this.subhandle = DividedCurve.MargeCurve(this.subhandle, subhandles[i]);
        }
    }

    public DividedCurve crease;
    public DividedCurve handle;
    public DividedCurve subhandle;






    public static DividedCurve PressCurve(DividedCurve curve, float[] pressures, Vector3 direction)
    {
        Quaternion toIndentity = FrenetFrame.TowardRotation(curve.Frames[0], FrenetFrame.Identity);
        Quaternion initRot = Quaternion.Lerp(Quaternion.identity, toIndentity, pressures[0]);
        DividedCurve twistCurve = DividedCurve.AllignCurve(curve, curve.Positions[0], initRot * curve.Frames[0]);
        Debug.Log(twistCurve.Frames[0].binormal);
        
        string text = "Press\ncurvature\ttorsion\n";
        for (int i = 0; i < twistCurve.Count - 1; i++)
        {
            FrenetFrame frame = twistCurve.Frames[i];
            FrenetFrame nextFrame = twistCurve.Frames[i + 1];
            float kappaRatio = 1.0f - Mathf.Abs(Vector3.Dot(curve.Frames[i].tangent, direction));
            float tauRatio = 1.0f - pressures[i];
            Quaternion toPlanarRotKappa = Quaternion.FromToRotation(nextFrame.tangent, frame.tangent);
            float sign = Mathf.Sign(Vector3.Dot(direction, curve.Frames[i].binormal));
            float nextSign = Mathf.Sign(Vector3.Dot(direction, curve.Frames[i + 1].binormal));
            Quaternion toPlanarRotTau = Quaternion.FromToRotation(nextFrame.binormal, frame.binormal * sign * nextSign);

            Quaternion rotKappa = Quaternion.Lerp(toPlanarRotKappa, Quaternion.identity, kappaRatio);
            Quaternion rotTau = Quaternion.Lerp(toPlanarRotTau, Quaternion.identity, tauRatio);
            text += curve.Curvatures[i] * kappaRatio +"\t" + curve.Torsions[i] * tauRatio + "\n";

            Quaternion rot = rotTau * rotKappa;
            for (int j = i + 1; j < twistCurve.Count; j++)
            {
                Vector3 v = rot * (twistCurve.Positions[j] - twistCurve.Positions[i]);
                twistCurve.Positions[j] = twistCurve.Positions[i] + v;
                twistCurve.Frames[j] = rot * twistCurve.Frames[j];
            }
        }
        text += curve.Curvatures[^1] + "\t" + curve.Torsions[^1] + "\n";
        Debug.Log(text);
        DividedCurve test = new DividedCurve(twistCurve.Positions);
        text = "reconst\ncurvature\ttorsion\n";
        for(int i = 0; i < curve.Count; i++)
        {
            text += test.Curvatures[i] + "\t" + test.Torsions[i] + "\n";
        }
        Debug.Log(text);

        return twistCurve;
    }

    public static DividedCurve PressCurve(DevelopableSurface surf, float[] pressures)
    {
        /*
        DividedCurve curve = surf.Curve;
        Quaternion toIndentity = FrenetFrame.TowardRotation(curve.Frames[0], FrenetFrame.Identity);
        Quaternion initRot = Quaternion.Lerp(Quaternion.identity, toIndentity, pressures[0]);
        DividedCurve pressedCurve = DividedCurve.AllignCurve(curve, curve.Positions[0], initRot * curve.Frames[0]);
        */
        DividedCurve pressedCurve = new DividedCurve(surf.Curve);

        for (int i = 0; i < pressedCurve.Count - 1; i++)
        {
            FrenetFrame frame = pressedCurve.Frames[i];
            FrenetFrame nextFrame = pressedCurve.Frames[i + 1];
            float tauRatio = 1.0f - pressures[i];
            float sign = Mathf.Sign(surf.Alpha[i].y);
            float nextSign = Mathf.Sign(surf.Alpha[i + 1].y);
            Quaternion toPlanarRotTau = Quaternion.FromToRotation(nextFrame.binormal, frame.binormal * sign * nextSign);
            Quaternion rotTau = Quaternion.Lerp(toPlanarRotTau, Quaternion.identity, tauRatio);

            for (int j = i + 1; j < pressedCurve.Count; j++)
            {
                Vector3 v = rotTau * (pressedCurve.Positions[j] - pressedCurve.Positions[i]);
                pressedCurve.Positions[j] = pressedCurve.Positions[i] + v;
                pressedCurve.Frames[j] = rotTau * pressedCurve.Frames[j];
            }
        }

        return pressedCurve;
    }

    public static List<int> GetNonInflectionPointRange(DevelopableSurface surf, int Index)
    {
        List<int> index = new List<int>() { Index };
        float sign = Mathf.Sign(surf.Alpha[Index].y);
        for (int i = Index - 1; 0 <= i; i--)
        {
            float currentSign = Mathf.Sign(surf.Alpha[i].y);
            if(currentSign != sign)
                break;
            index.Insert(0, i);
        }
        for (int i = Index + 1; i < surf.Curve.Count; i++)
        {
            float currentSign = Mathf.Sign(surf.Alpha[i].y);
            if (currentSign != sign)
                break;
            index.Add(i);
        }
        return index;
    }
}
