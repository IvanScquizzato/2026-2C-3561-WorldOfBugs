using System;
using Microsoft.Xna.Framework;

public class ShadowCasterHull
{
    private readonly Plane[] _planes = new Plane[13];
    private int _planeCount;

    private readonly Vector3[] _corners = new Vector3[BoundingFrustum.CornerCount];
    private readonly Plane[] _frustumPlanes = new Plane[6];
    private readonly bool[] _facesAwayFromLight = new bool[6];

    public BoundingFrustum CameraFrustum { get; private set; }

    private static readonly (int v0, int v1, int faceA, int faceB)[] FrustumEdges =
    {
        (0, 1, 0, 4), (1, 2, 0, 3), (2, 3, 0, 5), (3, 0, 0, 2),
        (4, 5, 1, 4), (5, 6, 1, 3), (6, 7, 1, 5), (7, 4, 1, 2),
        (0, 4, 4, 2), (1, 5, 4, 3), (2, 6, 5, 3), (3, 7, 5, 2)
    };

    /// <param name="directionToSun">Vector normalizado DESDE la escena HACIA el sol.</param>
    public void Build(
        Matrix cameraView,
        float fovRadians,
        float aspectRatio,
        float nearPlane,
        float maxShadowDistance,
        Vector3 directionToSun,
        float maxCasterDistance = 7500f)
    {
        _planeCount = 0;
        Vector3 toLight = Vector3.Normalize(directionToSun);
        Vector3 lightDirection = -toLight;

        Matrix shadowProj = Matrix.CreatePerspectiveFieldOfView(fovRadians, aspectRatio, nearPlane, maxShadowDistance);
        CameraFrustum = new BoundingFrustum(cameraView * shadowProj);
        CameraFrustum.GetCorners(_corners);

        Vector3 frustumCenter = Vector3.Zero;
        for (int i = 0; i < 8; i++)
            frustumCenter += _corners[i];
        frustumCenter /= 8f;

        _frustumPlanes[0] = CameraFrustum.Near;
        _frustumPlanes[1] = CameraFrustum.Far;
        _frustumPlanes[2] = CameraFrustum.Left;
        _frustumPlanes[3] = CameraFrustum.Right;
        _frustumPlanes[4] = CameraFrustum.Top;
        _frustumPlanes[5] = CameraFrustum.Bottom;

        // 1. Conservar caras del frustum que apuntan en contra de la luz
        for (int i = 0; i < 6; i++)
        {
            bool awayFromLight = Vector3.Dot(_frustumPlanes[i].Normal, lightDirection) >= 0f;
            _facesAwayFromLight[i] = awayFromLight;

            if (awayFromLight)
                _planes[_planeCount++] = _frustumPlanes[i];
        }

        // 2. Extruir aristas de silueta en precisión doble (double) para evitar error numérico
        double lx = toLight.X, ly = toLight.Y, lz = toLight.Z;

        for (int i = 0; i < FrustumEdges.Length; i++)
        {
            var edge = FrustumEdges[i];
            if (_facesAwayFromLight[edge.faceA] == _facesAwayFromLight[edge.faceB])
                continue;

            Vector3 a = _corners[edge.v0];
            Vector3 b = _corners[edge.v1];

            double ex = (double)b.X - a.X;
            double ey = (double)b.Y - a.Y;
            double ez = (double)b.Z - a.Z;
            double edgeLen = Math.Sqrt(ex * ex + ey * ey + ez * ez);
            if (edgeLen < 1e-6)
                continue;

            // Normalizar dirección de la arista antes del producto cruz
            ex /= edgeLen;
            ey /= edgeLen;
            ez /= edgeLen;

            // Cross(edgeDir, toLight) en double
            double nx = ey * lz - ez * ly;
            double ny = ez * lx - ex * lz;
            double nz = ex * ly - ey * lx;
            double nLen = Math.Sqrt(nx * nx + ny * ny + nz * nz);

            // Si sin(ángulo) < 0.001 (~0.05°), la arista es paralela a los rayos de luz
            if (nLen < 1e-3)
                continue;

            nx /= nLen;
            ny /= nLen;
            nz /= nLen;

            // Asegurar que la normal apunte hacia AFUERA del volumen
            double toOutsideX = (double)a.X - frustumCenter.X;
            double toOutsideY = (double)a.Y - frustumCenter.Y;
            double toOutsideZ = (double)a.Z - frustumCenter.Z;
            if (nx * toOutsideX + ny * toOutsideY + nz * toOutsideZ < 0.0)
            {
                nx = -nx;
                ny = -ny;
                nz = -nz;
            }

            float d = (float)(-(nx * a.X + ny * a.Y + nz * a.Z));
            _planes[_planeCount++] = new Plane(new Vector3((float)nx, (float)ny, (float)nz), d);
        }

        // 3. Plano de cierre hacia el sol
        float maxProjection = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            float proj = Vector3.Dot(_corners[i], toLight);
            if (proj > maxProjection)
                maxProjection = proj;
        }

        Vector3 capPoint = toLight * (maxProjection + maxCasterDistance);
        _planes[_planeCount++] = new Plane(capPoint, toLight);
    }

    public bool Intersects(in BoundingBox box)
    {
        // Si está dentro del campo de visión de la cámara, siempre proyecta sombra
        if (CameraFrustum.Intersects(box))
            return true;
        const float epsilon = 0f;

        // Si está fuera de cámara, verificar si su volumen proyectado entra al frustum
        for (int i = 0; i < _planeCount; i++)
        {
            Plane plane = _planes[i];

            Vector3 pVertex = new Vector3(
                plane.Normal.X >= 0f ? box.Min.X : box.Max.X,
                plane.Normal.Y >= 0f ? box.Min.Y : box.Max.Y,
                plane.Normal.Z >= 0f ? box.Min.Z : box.Max.Z
            );

            if (Vector3.Dot(plane.Normal, pVertex) + plane.D > epsilon)
                return false;
        }

        return true;
    }
}