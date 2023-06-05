layout(std140) uniform Camera_Matrices 
{
    mat4 View;
    mat4 Projection;
    mat4 ViewProjection;
    vec3 CameraPosition;
};

