#version 330 core

in vec2 vUv;
out vec4 FragColor;

uniform sampler2D uBg;

uniform sampler2D gAlbedo;
uniform sampler2D gNormal;
uniform sampler2D gWorldPos;
uniform sampler2D gMisc;

uniform float timeOfDay;

const float PI = 3.14159265359;

const float ambientBias = 4.5;
const float fogDensity = 0.0005;
const vec3 shadowColor = vec3(0.53, 0.81, 0.92);

vec3 unpackNormal(vec4 enc)
{
    return normalize(enc.xyz * 2.0 - 1.0);
}

void main()
{
    vec4 bg = texture(uBg, vUv);

    vec4 albedo = texture(gAlbedo, vUv);
    vec4 nEnc   = texture(gNormal, vUv);
    vec4 wpos   = texture(gWorldPos, vUv);
    vec4 misc   = texture(gMisc, vUv);

    float angle = timeOfDay * 2.0 * PI;
    vec3 lightDir = normalize(vec3(cos(angle), sin(angle), 0.5));
    float dayFactor = clamp(lightDir.y * 0.5 + 0.5, 0.0, 1.0);

    vec3 Normal = unpackNormal(nEnc);
    float vAO = misc.x;
    float dist = misc.y;

    vec3 fogColor = bg.rgb;
    float fogFactor = 1.0 - exp(-fogDensity * dist);
    fogFactor = clamp(fogFactor, 0.0, 1.0);
    

    vec3 ambientLight = normalize(vec3(ambientBias) + shadowColor) * pow(vAO, 0.8);
    float light = max(dot(normalize(Normal), lightDir), 0.0);
    vec3 wColor = albedo.rgb * (light * 0.2 + ambientLight * mix(0.4, 1.2, dayFactor));

    vec3 outColor = mix(bg.rgb, wColor, albedo.a);
    outColor = mix(outColor, fogColor, fogFactor);

    FragColor = vec4(outColor, 1.0);
}
