import VertexInput;

import ProwlFragmentInput;
import ProwlMeshInput;
import ProwlInstanceInput;
import ProwlVariables;


module Invalid;


[shader("vertex")]
float4 InvalidVS<Instance>(VertexInputs<ProwlVariables, ProwlMeshInput, Instance, void> inputs) : SV_Position
where optional Instance : IInstanceInput
{
    return inputs.ClipPosition();
}

[shader("fragment")]
float4 InvalidFS()
{
    return float4(1.0, 0.0, 1.0, 1.0);
}
