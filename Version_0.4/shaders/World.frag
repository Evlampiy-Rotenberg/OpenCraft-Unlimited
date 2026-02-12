#version 330 core

in float vAO;
in vec2 TexCoord;
in vec3 Normal;
in vec3 WorldPos;
in vec3 ViewPos;

layout (location = 0) out vec4 gAlbedo;
layout (location = 1) out vec4 gNormal;
layout (location = 2) out vec4 gWorldPos;
layout (location = 3) out vec4 gMisc;

uniform sampler2D texture0;

void main()
{
    vec4 tex = texture(texture0, TexCoord);
    if (tex.a < 0.5) discard;

    gAlbedo = vec4(tex.rgb, 1.0);
    gNormal = vec4(normalize(Normal) * 0.5 + 0.5, 1.0);
    gWorldPos = vec4(WorldPos, 1.0);
    gMisc = vec4(vAO, length(ViewPos), 0.0, 1.0);
}
