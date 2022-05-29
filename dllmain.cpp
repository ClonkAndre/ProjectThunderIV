// Project Thunder by ItsClonkAndre
// Version 1.0

#include "ZHookBase.cpp"
#include "Random.h"
#include "bass.h"
#include <filesystem>

using namespace injector;

#pragma region Classes
class LightningBoltPoint {

public:

#pragma region Variables
	SDKVector3 Position;
	float Radius;
	float LightRadius;
#pragma endregion

#pragma region Constructor
	LightningBoltPoint(SDKVector3 pos, float radius, float lightRadius)
	{
		Position = pos;
		Radius = radius;
		LightRadius = lightRadius;
	}
#pragma endregion

};
class AudioStream {

public:

#pragma region Variables
	HSTREAM Handle;
	HFX EchoEffectHandle, CompressorEffectHandle;
#pragma endregion

#pragma region Constructor
	AudioStream(HSTREAM handle)
	{
		Handle = handle;
		EchoEffectHandle = 0;
		CompressorEffectHandle = 0;
	}
	AudioStream()
	{
		Handle = 0;
		EchoEffectHandle = 0;
		CompressorEffectHandle = 0;
	}
#pragma endregion

	// - Echo Effect -
	void AddEchoEffect(int priority, float wetDryMix, float feedback, float leftDelay, float rightDelay, bool panDelay, bool expandLength = true, float expandBy = 8)
	{
		if (Handle == 0)
			return;
		if (EchoEffectHandle != 0)
			return;

		EchoEffectHandle = BASS_ChannelSetFX(Handle, BASS_FX_DX8_ECHO, priority);
		if (EchoEffectHandle != 0) {
			BASS_DX8_ECHO echoEffectOptions;
			echoEffectOptions.fWetDryMix = wetDryMix;
			echoEffectOptions.fFeedback = feedback;
			echoEffectOptions.fLeftDelay = leftDelay;
			echoEffectOptions.fRightDelay = rightDelay;
			echoEffectOptions.lPanDelay = panDelay;
			BASS_FXSetParameters(EchoEffectHandle, &echoEffectOptions);
			if (expandLength) BASS_ChannelSetAttribute(Handle, BASS_ATTRIB_TAIL, expandBy);
		}
	}
	void RemoveEchoEffect()
	{
		if (Handle == 0)
			return;
		if (EchoEffectHandle == 0)
			return;

		if (BASS_ChannelRemoveFX(Handle, EchoEffectHandle)) {
			EchoEffectHandle = 0;
		}
	}

	// - Compressor Effect -
	void AddCompressorEffect(int priority, float gain, float attack, float release, float threshold, float ratio, float predelay)
	{
		if (Handle == 0)
			return;
		if (CompressorEffectHandle != 0)
			return;

		CompressorEffectHandle = BASS_ChannelSetFX(Handle, BASS_FX_DX8_COMPRESSOR, priority);
		if (CompressorEffectHandle != 0) {
			BASS_DX8_COMPRESSOR compressorEffectOptions;
			compressorEffectOptions.fGain = gain;
			compressorEffectOptions.fAttack = attack;
			compressorEffectOptions.fRelease = release;
			compressorEffectOptions.fThreshold = threshold;
			compressorEffectOptions.fRatio = ratio;
			compressorEffectOptions.fPredelay = predelay;
			BASS_FXSetParameters(CompressorEffectHandle, &compressorEffectOptions);
		}
	}
	void RemoveCompressorEffect()
	{
		if (Handle == 0)
			return;
		if (CompressorEffectHandle == 0)
			return;

		if (BASS_ChannelRemoveFX(Handle, CompressorEffectHandle)) {
			CompressorEffectHandle = 0;
		}
	}

};
enum class AudioPlayMode {
	APM_Unknown,
	APM_Play,
	APM_Pause,
	APM_Stop
};
#pragma endregion

#pragma region Variables

// Variables
std::vector<LightningBoltPoint> m_lightningBoltPoints;
std::vector<AudioStream> m_audioStreams;
Random m_rnd = Random(time(nullptr) * 1000);

Ped m_playerPed;
SDKVector3 m_playerPosition;
Cam m_mainCam;
float m_SFXVolume;

#pragma region Settings
// General
bool m_needsToBeInPlayersView;
int m_chanceOfAThunderBoltToHappen;

// Sound
int m_multiplier;
bool m_enable3D;
bool m_playSoundInCutscenes;

// Echo
bool m_enableEcho;
float m_echoWetDryMix;
float m_echoFeedback;
float m_echoLeftDelay, m_echoRightDelay;
bool m_echoPanDelay;

// Compressor
bool m_enableCompressor;
float m_compGain;
float m_compAttack;
float m_compRelease;
float m_compThreshold;
float m_compRatio;
float m_compPredelay;

// Height
int m_heightMin, m_heightMax;
bool m_shouldBeLowerIfDrivingAVehicle;
int m_drivingHeightMin, m_drivingHeightMax;

// Size
int m_sizeMin, m_sizeMax;

// Distance
int m_distMin, m_distMax;

// Explosion
bool m_createExplosion;
int m_expType;
float m_expCamShake, m_expRadius;

// Thunderbolt
int m_boltFadeOutSpeed;
int m_boltColorR, m_boltColorG, m_boltColorB;

// Light
bool m_shouldEmitLight;
int m_lightFadeOutSpeed;
int m_lightIntensity, m_lightRadius;
int m_lightColorR, m_lightColorG, m_lightColorB;
#pragma endregion

#pragma endregion

#pragma region Bass

inline bool SetStreamVolume(HSTREAM stream, float volume)
{
	if (stream != 0) return BASS_ChannelSetAttribute(stream, BASS_ATTRIB_VOL, volume / 100.0f);
	return false;
}
inline HSTREAM LoadAudioFile(const void* file, bool createWith3D = false)
{
	HSTREAM handle = 0;

	if (createWith3D) {
		handle = BASS_StreamCreateFile(false, file, 0, 0, BASS_STREAM_PRESCAN | BASS_STREAM_AUTOFREE | BASS_SAMPLE_MONO | BASS_SAMPLE_3D);
	}
	else {
		handle = BASS_StreamCreateFile(false, file, 0, 0, BASS_STREAM_PRESCAN | BASS_STREAM_AUTOFREE);
	}

	return handle;
}

inline bool ChangeStreamPlayMode(HSTREAM stream, AudioPlayMode newState, BOOL restart = false)
{
	if (stream != 0) {
		switch (newState) {
			case AudioPlayMode::APM_Play:
				return BASS_ChannelPlay(stream, restart);
			case AudioPlayMode::APM_Pause:
				return BASS_ChannelPause(stream);
			case AudioPlayMode::APM_Stop:
				return BASS_ChannelStop(stream);
			default:
				return false;
		}
	}
	return false;
}
inline AudioPlayMode GetStreamPlayMode(HSTREAM stream)
{
	if (stream != 0) {
		switch (BASS_ChannelIsActive(stream)) {
			case BASS_ACTIVE_PLAYING:
				return AudioPlayMode::APM_Play;
			case BASS_ACTIVE_PAUSED:
				return AudioPlayMode::APM_Pause;
			case BASS_ACTIVE_STOPPED:
				return AudioPlayMode::APM_Stop;
			default:
				return AudioPlayMode::APM_Unknown;
		}
	}
	return AudioPlayMode::APM_Unknown;
}

inline bool FreeStream(HSTREAM stream)
{
	if (stream != 0) {
		BASS_ChannelStop(stream);
		return BASS_StreamFree(stream);
	}
	return false;
}

inline int GetBassErrorCode()
{
	return BASS_ErrorGetCode();
}

// 3D
inline bool SetListener3DPosition(SDKVector3 pos, SDKVector3 front, SDKVector3 top)
{
	BASS_3DVECTOR bassPosVec(pos.x, pos.y, pos.z);
	BASS_3DVECTOR bassDirFrontVec(front.x, front.y, front.z);
	BASS_3DVECTOR bassDirTopVec(top.x, top.y, top.z);
	return BASS_Set3DPosition(&bassPosVec, NULL, &bassDirFrontVec, &bassDirTopVec);
}
inline bool SetStream3DPosition(HSTREAM stream, SDKVector3 soundPos)
{
	bool result = false;
	if (stream != 0) {
		BASS_3DVECTOR bassSoundPosVec(soundPos.x, soundPos.y, soundPos.z);
		result = BASS_ChannelSet3DPosition(stream, &bassSoundPosVec, NULL, NULL);
		BASS_Apply3D();
	}
	return result;
}

// Fading
inline bool FadeStreamOut(HSTREAM stream, AudioPlayMode after, int fadingSpeed = 1000)
{
	if (stream == 0)
		return false;

	if (!BASS_ChannelIsSliding(stream, BASS_ATTRIB_VOL)) {
		BASS_ChannelSlideAttribute(stream, BASS_ATTRIB_VOL, 0, fadingSpeed);
		return true;
	}

	return false;
}
inline bool FadeStreamIn(HSTREAM stream, float fadeToVolumeLevel, int fadingSpeed = 1000)
{
	if (stream == 0)
		return false;

	if (!BASS_ChannelIsSliding(stream, BASS_ATTRIB_VOL)) {
		BASS_ChannelPlay(stream, false);
		BASS_ChannelSlideAttribute(stream, BASS_ATTRIB_VOL, fadeToVolumeLevel / 100.0f, fadingSpeed);
		return true;
	}

	return false;
}

#pragma endregion

#pragma region Functions

// Filesystem
inline int GetFileCount(std::string path)
{
	auto dirIter = std::filesystem::directory_iterator(path);
	return std::count_if(begin(dirIter), end(dirIter), [](auto& entry) { return entry.is_regular_file(); });
}
inline int GetDirectoryCount(std::string path)
{
	auto dirIter = std::filesystem::directory_iterator(path);
	return std::count_if(begin(dirIter), end(dirIter), [](auto& entry) { return entry.is_directory(); });
}
inline bool DoesFileExists(const std::string& name)
{
	std::ifstream f(name.c_str());
	return f.good();
}

// Random
inline uint GetRandomIntInRange(uint min, uint max)
{
	uint value;
	GENERATE_RANDOM_INT_IN_RANGE(min, max, &value);
	return value;
}
inline float GetRandomFloatInRange(float min, float max)
{
	float value;
	GENERATE_RANDOM_FLOAT_IN_RANGE(min, max, &value);
	return value;
}
inline SDKVector3 GetPositionAroundPosition(SDKVector3 pos, float distance)
{
	SDKVector3 rndVec;
	rndVec.x = (float)(m_rnd.NextDouble() - 0.5) * distance;
	rndVec.y = (float)(m_rnd.NextDouble() - 0.5) * distance;
	rndVec.z = 0;
	return pos + rndVec;
}

// Converters
inline SDKVector3 HeadingToDirection(float heading)
{
	float h = heading * (M_PI / 180);
	SDKVector3 dir;
	dir.x = (float)-sin(h);
	dir.y = (float)cos(h);
	dir.z = 0;
	return dir;
}
inline SDKVector3 ConvertToLeftHandedVector(SDKVector3 org)
{
	SDKVector3 result;
	result.x = org.y;
	result.y = org.z;
	result.z = org.x;
	return result;
}

// Other
inline bool IsPlayerInAnyCar()
{
	return IS_CHAR_IN_ANY_CAR(m_playerPed);
}
inline bool IsCoordVisible(SDKVector3 pos, float radius = 1)
{
	return CAM_IS_SPHERE_VISIBLE(m_mainCam, pos.x, pos.y, pos.z, radius);
}
inline float GetGroundZFor3DCoord(SDKVector3 pos)
{
	float value;
	GET_GROUND_Z_FOR_3D_COORD(pos.x, pos.y, pos.z, &value);
	return value;
}
inline float Get3DDistance(SDKVector3 pos, SDKVector3 pos2)
{
	SDKVector3 vector3_2 = pos2 - pos;
	float x2 = vector3_2.x;
	float y2 = vector3_2.y;
	float z2 = vector3_2.z;
	float num1 = y2;
	float num2 = num1 * num1;
	float num3 = x2;
	float num4 = num3 * num3;
	float num5 = num2 + num4;
	float num6 = z2;
	float num7 = num6 * num6;
	return sqrt(num5 + num7);
}
inline SDKVector3 GetPositionInFront(SDKVector3 pos, SDKVector3 dir, float multi = 2)
{
	return SDKVector3::Add(pos, SDKVector3::Mulitply(dir, multi));
}

#pragma endregion

#pragma region Methods
inline AudioStream PlayThunderSound(SDKVector3 pos)
{
	std::string path = ".\\ProjectThunder";
	int fileCount = GetFileCount(path);

	if (fileCount == 0)
		return 0;

	// Get random number for sound file
	uint rndNumber = GetRandomIntInRange(1, fileCount + 1);
	path += "\\";
	path += std::to_string(rndNumber);
	path += ".mp3";

	// Try to load audio file and set stuff
	AudioStream audioStream{ LoadAudioFile(path.c_str(), m_enable3D) };
	if (audioStream.Handle != 0) {

		// Set echo effect for handle
		if (m_enableEcho) audioStream.AddEchoEffect(2, m_echoWetDryMix, m_echoFeedback, m_echoLeftDelay, m_echoRightDelay, m_echoPanDelay);

		SetStreamVolume(audioStream.Handle, m_SFXVolume); // Sets the volume of the sound
		if (m_enable3D) SetStream3DPosition(audioStream.Handle, pos); // Set 3D position of sound
		ChangeStreamPlayMode(audioStream.Handle, AudioPlayMode::APM_Play); // Play audio file

	}
	return audioStream;
}
inline void SummonThunderbolt(bool playSound = true)
{
	m_lightningBoltPoints.clear();

	uint maxRandomPointCount = GetRandomIntInRange((uint)m_sizeMin, (uint)m_sizeMax);
	SDKVector3 playerAroundPos = GetPositionAroundPosition(m_playerPosition, GetRandomFloatInRange(m_distMin, m_distMax));

	if (m_needsToBeInPlayersView) {
		if (!IsCoordVisible(playerAroundPos))
			return;
	}

	// /summon lightning_bolt!
	for (uint i = 0; i < maxRandomPointCount; i++) {
		if (i == 0) { // Create thunderbolt starting point

			SDKVector3 pos;
			pos.x = playerAroundPos.x;
			pos.y = playerAroundPos.y;
			pos.z = GetRandomFloatInRange(m_shouldBeLowerIfDrivingAVehicle && IsPlayerInAnyCar() ? m_drivingHeightMin : m_heightMin, m_shouldBeLowerIfDrivingAVehicle && IsPlayerInAnyCar() ? m_drivingHeightMax : m_heightMax);
			m_lightningBoltPoints.push_back(LightningBoltPoint(pos, GetRandomFloatInRange(1500, 1900), m_lightRadius)); // Add starting point to list of points

		}
		else { // Create random thunderbolt points

			LightningBoltPoint previousPoint = m_lightningBoltPoints[i - 1];
			SDKVector3 pos;
			pos.x = previousPoint.Position.x;
			pos.y = previousPoint.Position.y;
			pos.z = previousPoint.Position.z - GetRandomFloatInRange(1, 2);
			m_lightningBoltPoints.push_back(LightningBoltPoint(GetPositionAroundPosition(pos, GetRandomFloatInRange(1, 5)), i >= (maxRandomPointCount - 15) ? 800 - 60 : 800, m_lightRadius));  // Add point to list of points

		}
	}

	// Play Sound
	if (playSound) {
		if (m_playSoundInCutscenes) {
			SDKVector3 pos = m_lightningBoltPoints[m_lightningBoltPoints.size() / 2].Position;
			AudioStream s = PlayThunderSound(ConvertToLeftHandedVector(pos));
			if (s.Handle != 0) m_audioStreams.push_back(s);
		}
		else {
			if (HAS_CUTSCENE_FINISHED()) {
				SDKVector3 pos = m_lightningBoltPoints[m_lightningBoltPoints.size() / 2].Position;
				AudioStream s = PlayThunderSound(ConvertToLeftHandedVector(pos));
				if (s.Handle != 0) m_audioStreams.push_back(s);
			}
		}
	}

	// Create explosion if strike hits the ground (Not perfect!)
	if (m_createExplosion) {
		LightningBoltPoint lastPoint = m_lightningBoltPoints[m_lightningBoltPoints.size() - 1];
		float groundZ = GetGroundZFor3DCoord(lastPoint.Position);
		if (lastPoint.Position.z <= groundZ) {

			for (int i = m_lightningBoltPoints.size() - 1; i >= 0; i--) {
				LightningBoltPoint p = m_lightningBoltPoints[i];
				if (p.Position.z >= groundZ) {
					lastPoint = p;
					break;
				}
				groundZ = GetGroundZFor3DCoord(p.Position);
			}
			ADD_EXPLOSION(lastPoint.Position.x, lastPoint.Position.y, lastPoint.Position.z, m_expType, m_expRadius, true, false, m_expCamShake);

		}
	}
}
#pragma endregion

#pragma region CoronaLimitAdjuster
// Corona Limit Adjuster for GTA IV from ThirteenAG's Project2DFX GitHub Repository
// https://github.com/ThirteenAG/III.VC.SA.IV.Project2DFX

// Copyright(c) 2017 ThirteenAG
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this softwareand associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and /or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright noticeand this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

inline void IncreaseCoronaLimit()
{
	auto nCoronasLimit = static_cast<DWORD>(3 * pow(2.0, 14)); // 49152, default 3 * pow(2, 8) = 768

	static std::vector<int> aCoronas;
	static std::vector<int> aCoronas2;
	aCoronas.resize(nCoronasLimit * 0x3C * 4);
	aCoronas2.resize(nCoronasLimit * 0x3C * 4);

	if (injector::address_manager::singleton().IsIV()) {
		AdjustPointer(aslr_ptr(0x7E19F4), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E19EE), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E0F95), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E1A02), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E1A10), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E1A74), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E1A7C), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E1A84), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E1A8D), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E1182), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E19A1), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E1A39), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E1A4C), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E1A5A), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E19C1), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E19DA), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E19B5), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E19AB), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E19CD), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E199A), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E1A22), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E1A60), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E1A93), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));
		AdjustPointer(aslr_ptr(0x7E1AAB), &aCoronas[0], aslr_ptr(0x116AB00), aslr_ptr(0x116AB00 + 0x3C));

		AdjustPointer(aslr_ptr(0x7E10AB), &aCoronas2[0], aslr_ptr(0x115EB00), aslr_ptr(0x115EB00 + 0x1B));
		AdjustPointer(aslr_ptr(0x7E14BA), &aCoronas2[0], aslr_ptr(0x115EB00), aslr_ptr(0x115EB00 + 0x1B));
		AdjustPointer(aslr_ptr(0x7E10B9), &aCoronas2[0], aslr_ptr(0x115EB00), aslr_ptr(0x115EB00 + 0x1B));
		AdjustPointer(aslr_ptr(0x7E10C7), &aCoronas2[0], aslr_ptr(0x115EB00), aslr_ptr(0x115EB00 + 0x1B));
		AdjustPointer(aslr_ptr(0x7E10CF), &aCoronas2[0], aslr_ptr(0x115EB00), aslr_ptr(0x115EB00 + 0x1B));
		AdjustPointer(aslr_ptr(0x7E14C2), &aCoronas2[0], aslr_ptr(0x115EB00), aslr_ptr(0x115EB00 + 0x1B));
		AdjustPointer(aslr_ptr(0x7E10FD), &aCoronas2[0], aslr_ptr(0x115EB00), aslr_ptr(0x115EB00 + 0x1B));
		AdjustPointer(aslr_ptr(0x7E14CA), &aCoronas2[0], aslr_ptr(0x115EB00), aslr_ptr(0x115EB00 + 0x1B));
		AdjustPointer(aslr_ptr(0x7E10D5), &aCoronas2[0], aslr_ptr(0x115EB00), aslr_ptr(0x115EB00 + 0x1B));
		AdjustPointer(aslr_ptr(0x7E1671), &aCoronas2[0], aslr_ptr(0x115EB00), aslr_ptr(0x115EB00 + 0x1B));
		AdjustPointer(aslr_ptr(0x7E10DB), &aCoronas2[0], aslr_ptr(0x115EB00), aslr_ptr(0x115EB00 + 0x1B));
		AdjustPointer(aslr_ptr(0x7E14A5), &aCoronas2[0], aslr_ptr(0x115EB00), aslr_ptr(0x115EB00 + 0x1B));
		AdjustPointer(aslr_ptr(0x7E10E5), &aCoronas2[0], aslr_ptr(0x115EB00), aslr_ptr(0x115EB00 + 0x1B));
		AdjustPointer(aslr_ptr(0x7E14AC), &aCoronas2[0], aslr_ptr(0x115EB00), aslr_ptr(0x115EB00 + 0x1B));
		AdjustPointer(aslr_ptr(0x7E10F5), &aCoronas2[0], aslr_ptr(0x115EB00), aslr_ptr(0x115EB00 + 0x1B));
		AdjustPointer(aslr_ptr(0x7E14B3), &aCoronas2[0], aslr_ptr(0x115EB00), aslr_ptr(0x115EB00 + 0x1B));
		AdjustPointer(aslr_ptr(0x7E1103), &aCoronas2[0], aslr_ptr(0x115EB00), aslr_ptr(0x115EB00 + 0x1B));
		AdjustPointer(aslr_ptr(0x7E1318), &aCoronas2[0], aslr_ptr(0x115EB00), aslr_ptr(0x115EB00 + 0x1B));

		WriteMemory<unsigned char>(aslr_ptr(0x7E109F + 0x2), 14);
		WriteMemory<unsigned char>(aslr_ptr(0x7E149A + 0x2), 14);
		WriteMemory<unsigned char>(aslr_ptr(0x7E130E + 0x2), 14);

		WriteMemory<unsigned int>(aslr_ptr(0x7E1979 + 0x2), nCoronasLimit);
		WriteMemory<unsigned int>(aslr_ptr(0x7E1072 + 0x2), nCoronasLimit);
		WriteMemory<unsigned int>(aslr_ptr(0x7E1189 + 0x1), nCoronasLimit * 64);
	}
}
#pragma endregion

void scriptLoad()
{
	// Initialize bass
	BASS_Init(-1, 44100, BASS_DEVICE_3D, 0, NULL);

	// Apply corona limit adjuster if Project2DFX is not installed
	if (!DoesFileExists(".\\IVLodLights.asi") && !DoesFileExists(".\\plugins\\IVLodLights.asi")) {
		IncreaseCoronaLimit();
	}

	// Load settings
    INI<> ini("ProjectThunder.ini", true);
    if (ini.select("General")) {
		m_needsToBeInPlayersView = std::stoi(ini.get("NeedsToBeInPlayersView", "0")) == 1;
		m_chanceOfAThunderBoltToHappen = std::stoi(ini.get("ChanceOfAThunderBoltToHappen", "4"));
    }
	if (ini.select("Sound")) {
		m_multiplier = std::stoi(ini.get("Multiplier", "30"));
		m_enable3D = std::stoi(ini.get("Enable3D", "1")) == 1;
		m_playSoundInCutscenes = std::stoi(ini.get("PlayInCutscenes", "1")) == 1;
	}
	if (ini.select("Echo")) {
		m_enableEcho = std::stoi(ini.get("Enable", "1")) == 1;
		m_echoWetDryMix = (float)std::stod(ini.get("WetDryMix", "50"));
		m_echoFeedback = (float)std::stod(ini.get("Feedback", "55"));
		m_echoLeftDelay = (float)std::stod(ini.get("LeftDelay", "430"));
		m_echoRightDelay = (float)std::stod(ini.get("RightDelay", "430"));
		m_echoPanDelay = std::stoi(ini.get("PanDelay", "0")) == 1;
	}
	if (ini.select("Compressor")) {
		m_enableCompressor = std::stoi(ini.get("Enable", "1")) == 1;
		m_compGain = (float)std::stod(ini.get("Gain", "-3"));
		m_compAttack = (float)std::stod(ini.get("Attack", "10"));
		m_compRelease = (float)std::stod(ini.get("Release", "1000"));
		m_compThreshold = (float)std::stod(ini.get("Threshold", "-15"));
		m_compRatio = (float)std::stod(ini.get("Ratio", "3"));
		m_compPredelay = (float)std::stod(ini.get("Predelay", "0"));
	}
	if (ini.select("Height")) {
		m_heightMin = std::stoi(ini.get("Min", "300"));
		m_heightMax = std::stoi(ini.get("Max", "550"));
		m_shouldBeLowerIfDrivingAVehicle = std::stoi(ini.get("ShouldBeLowerIfDrivingAVehicle", "0")) == 1;
		m_drivingHeightMin = std::stoi(ini.get("DrivingMin", "200"));
		m_drivingHeightMax = std::stoi(ini.get("DrivingMax", "400"));
	}
	if (ini.select("Size")) {
		m_sizeMin = std::stoi(ini.get("Min", "90"));
		m_sizeMax = std::stoi(ini.get("Max", "275"));
	}
	if (ini.select("Distance")) {
		m_distMin = std::stoi(ini.get("Min", "200"));
		m_distMax = std::stoi(ini.get("Max", "3000"));
	}
	if (ini.select("Explosion")) {
		m_createExplosion = std::stoi(ini.get("CreateExplosionIfThunderBoltHitsGround", "1")) == 1;
		m_expType = std::stoi(ini.get("Type", "7"));
		m_expRadius = (float)std::stod(ini.get("Radius", "250"));
		m_expCamShake = (float)std::stod(ini.get("CamShake", "1"));
	}
	if (ini.select("Thunderbolt")) {
		m_boltFadeOutSpeed = std::stoi(ini.get("FadeOutSpeed", "60"));
		m_boltColorR = std::stoi(ini.get("Red", "218"));
		m_boltColorG = std::stoi(ini.get("Green", "206"));
		m_boltColorB = std::stoi(ini.get("Blue", "233"));
	}
	if (ini.select("Light")) {
		m_shouldEmitLight = std::stoi(ini.get("ShouldThunderBoltEmitLight", "1")) == 1;
		m_lightFadeOutSpeed = std::stoi(ini.get("FadeOutSpeed", "2"));
		m_lightIntensity = std::stoi(ini.get("Intensity", "70"));
		m_lightRadius = std::stoi(ini.get("Radius", "150"));
		m_lightColorR = std::stoi(ini.get("Red", "130"));
		m_lightColorG = std::stoi(ini.get("Green", "92"));
		m_lightColorB = std::stoi(ini.get("Blue", "153"));
	}
}
void scriptUnload()
{
	BASS_Free();
}

void scriptTick()
{
#pragma region GetStuff
	Player player = CONVERT_INT_TO_PLAYERINDEX(GET_PLAYER_ID());
	GET_PLAYER_CHAR(player, &m_playerPed);

	// Get char coordinates
	float pX, pY, pZ;
	GET_CHAR_COORDINATES(m_playerPed, &pX, &pY, &pZ);
	m_playerPosition.x = pX;
	m_playerPosition.y = pY;
	m_playerPosition.z = pZ;

	// Get root camera and the position and rotation of it
	GET_ROOT_CAM(&m_mainCam);

	float camPosX, camPosY, camPosZ;
	float camRotX, camRotY, camRotZ;
	GET_CAM_POS(m_mainCam, &camPosX, &camPosY, &camPosZ);
	GET_CAM_ROT(m_mainCam, &camRotX, &camRotY, &camRotZ);

	SDKVector3 camPosition;
	camPosition.x = camPosX;
	camPosition.y = camPosY;
	camPosition.z = camPosZ;
	SDKVector3 camRotation;
	camRotation.x = camRotX;
	camRotation.y = camRotY;
	camRotation.z = camRotZ;

	// Get game SFX volume
	m_SFXVolume = *(uint32_t*)(baseAddress + ADDRESS_SETTINGS + (SETTING_SFX_LEVEL)) * m_multiplier;
#pragma endregion
	
	// Set audio stuff
	SetListener3DPosition(ConvertToLeftHandedVector(camPosition),
		ConvertToLeftHandedVector(HeadingToDirection(camRotation.z)),
		SDKVector3::Up());

	if (IsPlayerInAnyCar()) {
		BASS_Set3DFactors(0.3048, 0.02, 0.0);
	}
	else {
		BASS_Set3DFactors(0.3048, 0.03, 0.0);
	}

	// Set compression effect on all active thunder sounds when in building
	if (m_enableCompressor) {
		if (IS_INTERIOR_SCENE()) {
			for (int i = 0; i < m_audioStreams.size(); i++) {
				m_audioStreams[i].AddCompressorEffect(1, m_compGain, m_compAttack, m_compRelease, m_compThreshold, m_compRatio, m_compPredelay);
			}
		}
		else {
			for (int i = 0; i < m_audioStreams.size(); i++) {
				m_audioStreams[i].RemoveCompressorEffect();
			}
		}
	}

	BASS_Apply3D();

	// Draw Thunder
	for (int i = 0; i < m_lightningBoltPoints.size(); i++) {
		// Draw all points as coronas
		LightningBoltPoint boltPoint = m_lightningBoltPoints[i];
		DRAW_CORONA_2(boltPoint.Position.x, boltPoint.Position.y, boltPoint.Position.z, boltPoint.Radius, 0, 0, m_boltColorR, m_boltColorG, m_boltColorB);

		// Draw light with range
		if (m_shouldEmitLight) DRAW_LIGHT_WITH_RANGE_2(boltPoint.Position.x, boltPoint.Position.y, boltPoint.Position.z, m_lightColorR, m_lightColorG, m_lightColorB, m_lightIntensity, boltPoint.LightRadius);

		// Fade out
		if (!(boltPoint.Radius <= 0)) {
			m_lightningBoltPoints[i].Radius -= m_boltFadeOutSpeed;
			if (!(boltPoint.LightRadius <= 0)) m_lightningBoltPoints[i].LightRadius -= m_lightFadeOutSpeed;
		}
		else {
			m_lightningBoltPoints.erase(m_lightningBoltPoints.begin() + i);
		}
	}

	// Fade active streams out and pauses them if pause menu is active
	if (IS_PAUSE_MENU_ACTIVE()) {
		for (int i = 0; i < m_audioStreams.size(); i++) {
			AudioStream s = m_audioStreams[i];

			if (s.Handle == 0) {
				m_audioStreams.erase(m_audioStreams.begin() + i);
				continue;
			}

			if (GetStreamPlayMode(s.Handle) == AudioPlayMode::APM_Play) FadeStreamOut(s.Handle, AudioPlayMode::APM_Pause, 250);
		}
		return;
	}
	else {
		for (int i = 0; i < m_audioStreams.size(); i++) {
			AudioStream s = m_audioStreams[i];

			if (s.Handle == 0) {
				m_audioStreams.erase(m_audioStreams.begin() + i);
				continue;
			}

			switch (GetStreamPlayMode(s.Handle)) {
				case AudioPlayMode::APM_Pause:
					FadeStreamIn(s.Handle, m_SFXVolume, 250);
					break;
				case AudioPlayMode::APM_Stop:
					m_audioStreams.erase(m_audioStreams.begin() + i);
					break;
			}
		}
	}

	// Summon random thunderbolt when current weather is thunder
	eWeather currentWeather;
	GetCurrentWeather(&currentWeather);
	if (currentWeather == eWeather::WEATHER_LIGHTNING) {
		if (GetRandomFloatInRange(0, 1000) < m_chanceOfAThunderBoltToHappen) {
			SummonThunderbolt();
		}
	}
}