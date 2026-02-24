using System.IO;
using System.Numerics;

namespace LibreLancer.Media;

enum AudioEvent
{
    Play,
    Stop,
    Set3D,
    SetPosition,
    SetVelocity,
    SetAttenuation,
    SetPitch,
    SetDistance,
    SetCone,
    SetDirection,
    SetListenerPosition,
    SetListenerVelocity,
    SetListenerOrientation,
    StopAll,
    SetMasterGain,
    SetCategoryGain,
    MusicPlay,
    MusicStop,
    Quit
}

struct AudioEventMessage
{
    public AudioEvent Type;
    public SoundInstance Instance;
    public Stream Stream;
    public Vector3 Data;
    public Vector3 Data2;
}
