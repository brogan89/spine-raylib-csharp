using System.Numerics;
using Raylib_CSharp;
using Raylib_CSharp.Colors;
using Raylib_CSharp.Rendering;
using Raylib_CSharp.Rendering.Gl;
using Raylib_CSharp.Textures;
using Spine;
using BlendMode = Spine.BlendMode;

namespace RayLibTest;

/// <summary>
/// Src: https://github.com/jihiggins/spine-raylib-runtimes/blob/master/src/spine-raylib.h
/// </summary>
public class SpineRaylib
{
    private static readonly int[] _vertexOrderNormal = [0, 1, 2, 4];
    private static readonly int[] _vertexOrderReverse = [4, 2, 1, 0];

    private const int MAX_VERTICES_PER_ATTACHMENT = 2048;
    private readonly float[] _worldVerticesPositions = new float[MAX_VERTICES_PER_ATTACHMENT];
    private readonly Vertex[] _vertices = new Vertex[MAX_VERTICES_PER_ATTACHMENT];

    private float _anti_z_fighting_index;

    /// <summary>
    ///  For 3D set to -1f
    /// </summary>
    public float SP_LAYER_SPACING_BASE { get; set; }

    /// <summary>
    /// For 3D set to 0.5f
    /// </summary>
    public float SP_LAYER_SPACING { get; set; }

    /// <summary>
    /// Configure spine to draw double faced and to minimize zfigting artifacts
    /// </summary>
    public bool SP_DRAW_DOUBLE_FACED { get; set; }
    public bool SP_RENDER_WIREFRAME { get; set; }

    private struct Vertex
    {
        // Position in x/y plane
        // csharpier-ignore
        public float x, y;

        // UV coordinates
        // csharpier-ignore
        public float u, v;

        // Color, each channel in the range from 0-1
        // (Should really be a 32-bit RGBA packed color)
        // csharpier-ignore
        public float r, g, b, a;
    }

    private void AddVertex(
        float x,
        float y,
        float u,
        float v,
        float r,
        float g,
        float b,
        float a,
        ref int index
    )
    {
        _vertices[index].x = x;
        _vertices[index].y = y;
        _vertices[index].u = u;
        _vertices[index].v = v;
        _vertices[index].r = r;
        _vertices[index].g = g;
        _vertices[index].b = b;
        _vertices[index].a = a;
        index++;
    }

    public void DrawSkeleton(Skeleton skeleton, Vector3 position, bool pma)
    {
        // For each slot in the draw order array of the skeleton
        _anti_z_fighting_index = SP_LAYER_SPACING_BASE;
        for (int i = 0; i < skeleton.DrawOrder.Count; i++)
        {
            _anti_z_fighting_index -= SP_LAYER_SPACING;
            var slot = skeleton.DrawOrder.Items[i];

            // Fetch the currently active attachment, continue
            // with the next slot in the draw order if no
            // attachment is active on the slot
            var attachment = slot.Attachment;
            if (attachment is null)
                continue;

            // Calculate the tinting color based on the skeleton's color
            // and the slot's color. Each color channel is given in the
            // range [0-1], you may have to multiply by 255 and cast to
            // and int if your engine uses integer ranges for color channels.
            var tintA = skeleton.A * slot.A;
            var alpha = pma ? tintA : 1;
            var tintR = skeleton.R * slot.R * alpha;
            var tintG = skeleton.G * slot.G * alpha;
            var tintB = skeleton.B * slot.B * alpha;

            // Fill the vertices array depending on the type of attachment
            Texture2D texture;
            var vertexIndex = 0;

            switch (attachment)
            {
                // Cast to an spRegionAttachment so we can get the rendererObject
                // and compute the world vertices
                case RegionAttachment regionAttachment:

                    {
                        // Our engine specific Texture is stored in the spAtlasRegion which was
                        // assigned to the attachment on load. It represents the texture atlas
                        // page that contains the image the region attachment is mapped to
                        texture = (Texture2D)
                            ((AtlasRegion)regionAttachment.Region).page.rendererObject;

                        // Computed the world vertices positions for the 4 vertices that make up
                        // the rectangular region attachment. This assumes the world transform of the
                        // bone to which the slot (and hence attachment) is attached has been calculated
                        // before rendering via spSkeleton_updateWorldTransform
                        regionAttachment.ComputeWorldVertices(slot, _worldVerticesPositions, 0);

                        // Create 2 triangles, with 3 vertices each from the region's
                        // world vertex positions and its UV coordinates (in the range [0-1]).
                        AddVertex(
                            _worldVerticesPositions[0],
                            _worldVerticesPositions[1],
                            regionAttachment.UVs[0],
                            regionAttachment.UVs[1],
                            tintR,
                            tintG,
                            tintB,
                            tintA,
                            ref vertexIndex
                        );

                        AddVertex(
                            _worldVerticesPositions[2],
                            _worldVerticesPositions[3],
                            regionAttachment.UVs[2],
                            regionAttachment.UVs[3],
                            tintR,
                            tintG,
                            tintB,
                            tintA,
                            ref vertexIndex
                        );

                        AddVertex(
                            _worldVerticesPositions[4],
                            _worldVerticesPositions[5],
                            regionAttachment.UVs[4],
                            regionAttachment.UVs[5],
                            tintR,
                            tintG,
                            tintB,
                            tintA,
                            ref vertexIndex
                        );

                        AddVertex(
                            _worldVerticesPositions[4],
                            _worldVerticesPositions[5],
                            regionAttachment.UVs[4],
                            regionAttachment.UVs[5],
                            tintR,
                            tintG,
                            tintB,
                            tintA,
                            ref vertexIndex
                        );

                        AddVertex(
                            _worldVerticesPositions[6],
                            _worldVerticesPositions[7],
                            regionAttachment.UVs[6],
                            regionAttachment.UVs[7],
                            tintR,
                            tintG,
                            tintB,
                            tintA,
                            ref vertexIndex
                        );

                        AddVertex(
                            _worldVerticesPositions[0],
                            _worldVerticesPositions[1],
                            regionAttachment.UVs[0],
                            regionAttachment.UVs[1],
                            tintR,
                            tintG,
                            tintB,
                            tintA,
                            ref vertexIndex
                        );

                        BeginBlendMode(pma, slot);
                        var vertexOrder =
                            (skeleton.ScaleX * skeleton.ScaleY < 0)
                                ? _vertexOrderNormal
                                : _vertexOrderReverse;
                        Engine_DrawRegion(_vertices, texture, position, vertexOrder);
                    }
                    break;

                // Cast to an spMeshAttachment so we can get the rendererObject
                // and compute the world vertices
                case MeshAttachment mesh:

                    {
                        // Check the number of vertices in the mesh attachment. If it is bigger
                        // than our scratch buffer, we don't render the mesh. We do this here
                        // for simplicity, in production you want to reallocate the scratch buffer
                        // to fit the mesh.
                        if (mesh.WorldVerticesLength > MAX_VERTICES_PER_ATTACHMENT)
                            continue;

                        // Our engine specific Texture is stored in the spAtlasRegion which was
                        // assigned to the attachment on load. It represents the texture atlas
                        // page that contains the image the mesh attachment is mapped to
                        texture = (Texture2D)((AtlasRegion)mesh.Region).page.rendererObject;

                        // Computed the world vertices positions for the vertices that make up
                        // the mesh attachment. This assumes the world transform of the
                        // bone to which the slot (and hence attachment) is attached has been calculated
                        // before rendering via spSkeleton_updateWorldTransform
                        mesh.ComputeWorldVertices(slot, _worldVerticesPositions);

                        // Mesh attachments use an array of vertices, and an array of indices to define which
                        // 3 vertices make up each triangle. We loop through all triangle indices
                        // and simply emit a vertex for each triangle's vertex.
                        for (int j = 0; j < mesh.Triangles.Length; ++j)
                        {
                            var index = mesh.Triangles[j] << 1;
                            AddVertex(
                                _worldVerticesPositions[index],
                                _worldVerticesPositions[index + 1],
                                mesh.UVs[index],
                                mesh.UVs[index + 1],
                                tintR,
                                tintG,
                                tintB,
                                tintA,
                                ref vertexIndex
                            );
                        }

                        BeginBlendMode(pma, slot);
                        // Draw the mesh we created for the attachment
                        Engine_DrawMesh(_vertices, 0, vertexIndex, texture, position);
                    }

                    break;
            }
            Graphics.EndBlendMode();
        }
    }

    private static void BeginBlendMode(bool pma, Slot slot)
    {
        Graphics.EndBlendMode(); //Need this line for blending to work for some reason
        Raylib_CSharp.Rendering.BlendMode blendMode = slot.Data.BlendMode switch
        {
            BlendMode.Normal
                => pma
                    ? Raylib_CSharp.Rendering.BlendMode.Additive
                    : Raylib_CSharp.Rendering.BlendMode.Alpha,
            BlendMode.Additive
                => pma
                    ? Raylib_CSharp.Rendering.BlendMode.Additive
                    : Raylib_CSharp.Rendering.BlendMode.Alpha,
            BlendMode.Multiply => Raylib_CSharp.Rendering.BlendMode.Multiplied,
            BlendMode.Screen => Raylib_CSharp.Rendering.BlendMode.Additive,
            _ => throw new ArgumentOutOfRangeException()
        };
        RlGl.SetBlendMode(blendMode);
        Graphics.BeginBlendMode(Raylib_CSharp.Rendering.BlendMode.Custom);
        // Graphics.BeginBlendMode(blendMode);
    }

    private void Engine_DrawRegion(
        Vertex[] vertices,
        Texture2D texture,
        Vector3 position,
        int[] vertex_order
    )
    {
        RlGl.EnableTexture(texture.Id);
        RlGl.PushMatrix();
        {
            RlGl.Begin(DrawMode.Quads);
            {
                RlGl.Normal3F(0.0f, 0.0f, 1.0f);
                for (int i = 0; i < 4; i++)
                {
                    var vertex = vertices[vertex_order[i]];
                    RlGl.TexCoord2F(vertex.u, vertex.v);
                    RlGl.Color4F(vertex.r, vertex.g, vertex.b, vertex.a);
                    RlGl.Vertex3F(
                        position.X + vertex.x,
                        position.Y + vertex.y,
                        position.Z + _anti_z_fighting_index
                    );
                }
            }
            RlGl.End();

            if (SP_DRAW_DOUBLE_FACED)
            {
                RlGl.Begin(DrawMode.Quads);
                {
                    RlGl.Normal3F(0.0f, 0.0f, 1.0f);
                    for (int i = 3; i >= 0; i--)
                    {
                        var vertex = vertices[vertex_order[i]];
                        RlGl.TexCoord2F(vertex.u, vertex.v);
                        RlGl.Color4F(vertex.r, vertex.g, vertex.b, vertex.a);
                        RlGl.Vertex3F(
                            position.X + vertex.x,
                            position.Y + vertex.y,
                            position.X - _anti_z_fighting_index
                        );
                    }
                }
                RlGl.End();
            }
        }
        RlGl.PopMatrix();
        RlGl.DisableTexture();
    }

    private void Engine_DrawMesh(
        Vertex[] vertices,
        int start,
        int count,
        Texture2D texture,
        Vector3 position
    )
    {
        var vertex = new Vertex();
        RlGl.PushMatrix();
        {
            for (int vertexIndex = start; vertexIndex < count; vertexIndex += 3)
            {
                RlGl.EnableTexture(texture.Id);
                RlGl.Begin(DrawMode.Quads);
                {
                    int i;
                    for (i = 2; i > -1; i--)
                    {
                        vertex = vertices[vertexIndex + i];
                        RlGl.TexCoord2F(vertex.u, vertex.v);
                        RlGl.Color4F(vertex.r, vertex.g, vertex.b, vertex.a);
                        RlGl.Vertex3F(
                            position.X + vertex.x,
                            position.Y + vertex.y,
                            position.Z + _anti_z_fighting_index
                        );
                    }
                    RlGl.Vertex3F(
                        position.X + vertex.x,
                        position.Y + vertex.y,
                        position.Z + _anti_z_fighting_index
                    );
                }
                RlGl.End();

                if (SP_RENDER_WIREFRAME)
                {
                    Graphics.DrawTriangleLines(
                        new Vector2(
                            vertices[vertexIndex].x + position.X,
                            vertices[vertexIndex].y + position.Y
                        ),
                        new Vector2(
                            vertices[vertexIndex + 1].x + position.X,
                            vertices[vertexIndex + 1].y + position.Y
                        ),
                        new Vector2(
                            vertices[vertexIndex + 2].x + position.X,
                            vertices[vertexIndex + 2].y + position.Y
                        ),
                        vertexIndex == 0 ? Color.Red : Color.Green
                    );
                }
            }
        }
        RlGl.PopMatrix();
        RlGl.DisableTexture();
    }
}
