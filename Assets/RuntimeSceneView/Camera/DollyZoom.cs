using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeSceneView.Cameras
{
    public class DollyZoom
    {
        readonly float projectionConversionSize;

        public DollyZoom()
        {
            projectionConversionSize = SizePerDistance(60.0f);
        }

        public DollyZoom(float fieldOfView)
        {
            projectionConversionSize = SizePerDistance(fieldOfView);
        }


        float SizePerDistance(float fieldOfView)
        {
            return Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
        }

        float GetSizeRatio(float fieldOfView)
        {
            return projectionConversionSize / SizePerDistance(fieldOfView);
        }

        public float DollyZoomPerspectiveDistance(float worldDistance, float fieldOfView)
        {
            return worldDistance * GetSizeRatio(fieldOfView);
        }

        public float DollyZoomOrthographicSize(float worldDistance)
        {
            return worldDistance * projectionConversionSize;
        }
    }

}