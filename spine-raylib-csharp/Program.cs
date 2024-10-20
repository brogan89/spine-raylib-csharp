using System.Numerics;
using Raylib_CSharp;
using Raylib_CSharp.Camera.Cam3D;
using Raylib_CSharp.Colors;
using Raylib_CSharp.Interact;
using Raylib_CSharp.Rendering;
using Raylib_CSharp.Windowing;
using RayLibTest;
using Spine;

Window.Init(1280, 720, "Basic Window");
Time.SetTargetFPS(60);

// Define camera
// Define the camera to look into our 3d world
var camera = new Camera3D(
    new Vector3(0, 50, 200),
    new Vector3(0, 10, 0),
    Vector3.UnitY,
    45,
    CameraProjection.Perspective
);

const string ATLAS_PATH = "Content/SpineBoy/spineboy-pro.atlas";
const string JSON_PATH = "Content/SpineBoy/spineboy-pro.json";

// load atlas
var textureLoader = new RayLibTextureLoader();
var atlas = new Atlas(ATLAS_PATH, textureLoader);
var json = new SkeletonJson(atlas);
var skeletonData = json.ReadSkeletonData(JSON_PATH);

var skeleton = new Skeleton(skeletonData);
skeleton.SetSkin(skeletonData.DefaultSkin);
skeleton.ScaleX = 0.1f;
skeleton.ScaleY = 0.1f;

// animations
var animationStateData = new AnimationStateData(skeletonData);
var animationState = new AnimationState(animationStateData);
var idleAnimation = skeletonData.FindAnimation("idle");
animationState.AddAnimation(0, idleAnimation, true, 0);
animationState.Update(0);
animationState.Apply(skeleton);
skeleton.UpdateWorldTransform(Skeleton.Physics.None);

var spine = new SpineRaylib
{
    // SP_DRAW_DOUBLE_FACED = true,
    // SP_LAYER_SPACING = 0.5f,
    // SP_LAYER_SPACING_BASE = -1.0f,
    SP_RENDER_WIREFRAME = true
};

var showCursor = true;

while (!Window.ShouldClose())
{
    camera.Update(CameraMode.Custom);
    HandleInput();

    Graphics.BeginDrawing();
    {
        Graphics.ClearBackground(Color.DarkGray);

        Graphics.BeginMode3D(camera);
        {
            Graphics.DrawGrid(100, 5f);

            animationState.Update(Time.GetFrameTime());
            animationState.Apply(skeleton);
            skeleton.UpdateWorldTransform(Skeleton.Physics.None);
            spine.DrawSkeleton(skeleton, Vector3.Zero, atlas.Pages[0].pma);
        }
        Graphics.EndMode3D();

        Graphics.DrawFPS(10, 10);
    }

    Graphics.EndDrawing();
}

Window.Close();
return;

void HandleInput()
{
    if (Input.IsKeyPressed(KeyboardKey.C))
    {
        showCursor = !showCursor;
        if (showCursor)
            Input.ShowCursor();
        else
            Input.HideCursor();
    }
    if (Input.IsKeyPressed(KeyboardKey.Escape))
        Window.Close();
}
