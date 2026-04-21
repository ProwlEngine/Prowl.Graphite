Shader "ExampleShader"
{
    Properties
    {
        _Integer("Example integer", Integer) = 1
        _Color("Example color", Color) = (0.25, 0.5, 0.5, 1)
    }

    Pass 0
    {
        Name "Pass0"
        Tags { "PassID" = "Pass Zero" "IsPass0" = "True" }

        Blend One One

        ShaderSource "MaterialShader.slang"
        {
            Vertex "vertex"
            Fragment "fragment"
        }
    }

    Pass 1
    {
        Name "Pass1"
        Tags { "PassID" = "Pass One" "IsPass1" = "True" }

        ZTest Disabled

        ShaderSource "MaterialShader.slang"
        {
            Vertex "shadowVertex"
            Fragment "shadowFragment"
        }
    }

    Fallback "FallbackShader"
}
