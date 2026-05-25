using ChaosFramework.Shapes.Convex;
using ChaosFramework.Collections;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Constants;
using Plane = ChaosFramework.Math.Plane;

namespace ChaosFramework.Graphics
{
    public class Camera
    {
        static readonly Vector3f[] VIEW_FRUSTUM_VERTS = new Vector3f[] {
            Vector3f.EMPTY,
            new Vector3f(-1, 1, 1),
            new Vector3f(1, 1, 1),
            new Vector3f(-1, -1, 1),
            new Vector3f(1, -1, 1)
        };

        public Vector4i viewPort;

        public Vector3f Direction { get; private set; } = new Vector3f(0, 0, 1);
        public Vector3f Up { get; private set; } = new Vector3f(0, 1, 0);
        public Vector3f Position { get; private set; } = new Vector3f(0, 0, -1);

        public float tan { get; private set; }
        public float nearClip { get; private set; } = 0.05f;
        public float farClip { get; private set; } = 1000;
        public float verticalViewAngle { get; private set; } = PI_HALF / 2;
        public float screenRatio { get; private set; } = 1;

        public Matrix View { get; private set; }
        public Matrix Projection { get; private set; }
        public Matrix ViewProjection { get; private set; }
        public Matrix billBoard { get; private set; }
        public Matrix yBillBoard { get; private set; }
        public Matrix invView { get; private set; }
        public Matrix invViewProjection { get; private set; }

        public void SetPostProjectionMatrix(Matrix mat)
        {
            Projection = Matrix.PerspectiveFovLH(verticalViewAngle * 2, screenRatio, nearClip, farClip) * mat;
            UpdateImplicit();
        }

        public PointShape viewPointShape { get; private set; }
        public MeshShape viewFrustum { get; private set; }

        Plane[] viewFrustumPlanes = new Plane[4];
        public Plane frustomPlaneLeft => viewFrustumPlanes[0];
        public Plane frustomPlaneRight => viewFrustumPlanes[1];
        public Plane frustomPlaneTop => viewFrustumPlanes[2];
        public Plane frustomPlaneBottom => viewFrustumPlanes[3];

        public Vector3f GetLocalX() => Vector3f.Normalize(Vector3f.Cross(Up, Direction));
        public Vector3f GetLocalY() => Vector3f.Normalize(Vector3f.Cross(Direction, GetLocalX()));
        public Vector3f GetLocalZ() => Direction;

        public Camera()
        {
            viewFrustum = new MeshShape(VIEW_FRUSTUM_VERTS);
            viewPointShape = new PointShape(Position);
            Update();
        }

        public void Update(float nearClip = float.NaN, float farClip = float.NaN, float verticalViewAngle = float.NaN, float screenRatio = float.NaN)
        {
            if (!float.IsNaN(nearClip)) this.nearClip = nearClip;
            if (!float.IsNaN(farClip)) this.farClip = farClip;
            if (!float.IsNaN(verticalViewAngle)) this.verticalViewAngle = verticalViewAngle;
            if (!float.IsNaN(screenRatio)) this.screenRatio = screenRatio;
            Update();
        }

        /// <summary> Updates the view-matrix of this <see cref="Camera"/> with the given parameters. </summary>
        /// <param name="pos"> The position of the camera. </param>
        /// <param name="dir"> The view direction of this camera. </param>
        /// <param name="up"> The up-vector of this camera. </param>
        /// <param name="nearClip"> The near clip plane of this camera (if <see cref="float.NaN"/> this will not be changed). </param>
        /// <param name="farClip"> The far clip plane of this camera (if <see cref="float.NaN"/> this will not be changed). </param>
        /// <param name="verticalViewAngle"> The vertical view angle of this camera (if <see cref="float.NaN"/> this will not be changed). </param>
        /// <param name="screenRatio"> The screen ration of your render target (if <see cref="float.NaN"/> this will not be changed). </param>
        public void Update(
            Vector3f pos,
            Vector3f dir,
            Vector3f up,
            float nearClip = float.NaN,
            float farClip = float.NaN,
            float verticalViewAngle = float.NaN,
            float screenRatio = float.NaN
            )
        {
            Position = pos;
            Direction = Vector3f.Normalize(dir);
            Up = Vector3f.Normalize(up);
            if (!float.IsNaN(nearClip)) this.nearClip = nearClip;
            if (!float.IsNaN(farClip)) this.farClip = farClip;
            if (!float.IsNaN(verticalViewAngle)) this.verticalViewAngle = verticalViewAngle;
            if (!float.IsNaN(screenRatio)) this.screenRatio = screenRatio;
            Update();
        }

        public void Update()
        {
            viewPointShape = new PointShape(Position);
            Vector3f localX = GetLocalX();
            Vector3f localY = GetLocalY();
            Matrix m = Matrix.IDENTITY;
            m.m00 = localX.x; m.m01 = localX.y; m.m02 = localX.z;
            m.m10 = localY.x; m.m11 = localY.y; m.m12 = localY.z;
            m.m20 = Direction.x; m.m21 = Direction.y; m.m22 = Direction.z;
            billBoard = m;

            yBillBoard = GetLaserSprite(new Vector3f(0, 1, 0));

            View = Matrix.LookAtLH(Position, Position + Direction, localY);
            Projection = Matrix.PerspectiveFovLH(verticalViewAngle * 2, screenRatio, nearClip, farClip);
            UpdateImplicit();
            tan = (float)System.Math.Tan(verticalViewAngle);
        }

        void UpdateImplicit()
        {
            ViewProjection = View * Projection;

            invView = Matrix.Invert(View);
            invViewProjection = Matrix.Invert(ViewProjection);

            viewFrustum.Update(invViewProjection);
            viewFrustumPlanes[0] = Plane.Normalize(Plane.FromPoints(viewFrustum.transformedVerts[0], viewFrustum.transformedVerts[3], viewFrustum.transformedVerts[1]));
            viewFrustumPlanes[1] = Plane.Normalize(Plane.FromPoints(viewFrustum.transformedVerts[0], viewFrustum.transformedVerts[2], viewFrustum.transformedVerts[4]));
            viewFrustumPlanes[2] = Plane.Normalize(Plane.FromPoints(viewFrustum.transformedVerts[0], viewFrustum.transformedVerts[1], viewFrustum.transformedVerts[2]));
            viewFrustumPlanes[3] = Plane.Normalize(Plane.FromPoints(viewFrustum.transformedVerts[0], viewFrustum.transformedVerts[4], viewFrustum.transformedVerts[3]));
        }

        Matrix unjitteredProjection;
        public void ApplyJitter(int frameIndex)
        {
            var jitter = Halton.halton[frameIndex % Halton.halton.Length];
            var proj = unjitteredProjection = Projection;
            proj.m20 += jitter[0] / (2f * viewPort.z);
            proj.m21 += jitter[1] / (2f * viewPort.w);
            Projection = proj;
            UpdateImplicit();
        }
        public void UndoJitter()
        {
            Projection = unjitteredProjection;
            UpdateImplicit();
        }

        public bool CheckSphereIsVisible(Vector3f position, float radius)
        {
            for (int i = 0; i < viewFrustumPlanes.Length; i++)
                if (viewFrustumPlanes[i].Dot(position) > radius) return false;
            return true;
        }
        public bool CheckShapeIsVisible(Shape shape)
            => Shapes.Intersection.ConvexHull.CheckIntersection(viewFrustum, shape) != null;

        public bool CheckMeshIsVisible(params Vector3f[] verts)
            => CheckShapeIsVisible(new MeshShape(verts));

        public bool CheckBoundingBoxVisible(Vector3f low, Vector3f high)
            => CheckMeshIsVisible(
                low,
                new Vector3f(low.x, low.y, high.z), new Vector3f(low.x, high.y, low.z), new Vector3f(low.x, high.y, high.z),
                new Vector3f(high.x, low.y, low.z), new Vector3f(high.x, low.y, high.z), new Vector3f(high.x, high.y, low.z),
                high
                );

        public void SetClipPlanes(float nearPlane, float farPlane)
        {
            if (!float.IsNaN(nearPlane)) nearClip = nearPlane;
            if (!float.IsNaN(farPlane)) farClip = farPlane;

            Projection = Matrix.PerspectiveFovLH(verticalViewAngle, screenRatio, nearClip, farClip);
            ViewProjection = View * Projection;

            invViewProjection = Matrix.Invert(ViewProjection);
            viewFrustum.Update(invViewProjection);
        }

        public bool CheckVisible(LinkedList<Shape> shapes)
        {
            foreach (Shape shape in shapes)
                if (Shapes.Intersection.ConvexHull.CheckIntersection(shape, viewFrustum) != null)
                    return true;

            return false;
        }

        public bool CheckVisible(Shape shape)
            => Shapes.Intersection.ConvexHull.CheckIntersection(shape, viewFrustum) != null;

        public Matrix GetLaserSprite(Vector3f axis)
        {
            Matrix laser = Matrix.IDENTITY;
            axis.Normalize();
            Vector3f laserX = Vector3f.Normalize(Vector3f.Cross(axis, Direction));
            Vector3f laserZ = Vector3f.Cross(laserX, axis);
            laser.m00 = laserX.x; laser.m01 = laserX.y; laser.m02 = laserX.z;
            laser.m10 = axis.x; laser.m11 = axis.y; laser.m12 = axis.z;
            laser.m20 = laserX.x; laser.m21 = laserZ.y; laser.m22 = laserZ.z;
            return laser;
        }

        public static Matrix GetInvTransTransform(Matrix transform)
            => Matrix.Invert(Matrix.Transpose(transform));
    }
}
