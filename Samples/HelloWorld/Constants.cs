using Prowl.Graphite;
using Prowl.Vector;


public static class Constants
{

    public static Mesh CreateCube()
    {
        Mesh mesh = Mesh.Create();
        mesh.SetVertexInput<Float3>(s_meshVertices, 0);
        mesh.SetIndexInput16(s_meshIndices);

        return mesh;
    }

    private static Float3[] s_meshVertices = [
        new Float3(-0.5f, -0.5f, -0.5f), // 0
        new Float3( 0.5f, -0.5f, -0.5f), // 1
        new Float3( 0.5f,  0.5f, -0.5f), // 2
        new Float3(-0.5f,  0.5f, -0.5f), // 3
        new Float3(-0.5f, -0.5f,  0.5f), // 4
        new Float3( 0.5f, -0.5f,  0.5f), // 5
        new Float3( 0.5f,  0.5f,  0.5f), // 6
        new Float3(-0.5f,  0.5f,  0.5f)  // 7
    ];

    private static ushort[] s_meshIndices = [
        // Back face (-Z)
        0, 2, 1,
        0, 3, 2,

        // Front face (+Z)
        4, 5, 6,
        4, 6, 7,

        // Left face (-X)
        0, 7, 3,
        0, 4, 7,

        // Right face (+X)
        1, 2, 6,
        1, 6, 5,

        // Bottom face (-Y)
        0, 1, 5,
        0, 5, 4,

        // Top face (+Y)
        3, 7, 6,
        3, 6, 2
    ];
}
