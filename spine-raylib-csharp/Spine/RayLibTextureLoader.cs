using Raylib_CSharp.Textures;
using Spine;

namespace RayLibTest;

public class RayLibTextureLoader : TextureLoader
{
    public void Load(AtlasPage page, string path)
    {
        var texture = Texture2D.Load(path);
        page.rendererObject = texture;
    }

    public void Unload(object texture) { }
}
