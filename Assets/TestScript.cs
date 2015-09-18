using UnityEngine;
using System.Collections;

using LibSequentia.Engine;
using LibSequentia.Data;

public class TestScript : MonoBehaviour
{
	[SerializeField]
	UnityEngine.Audio.AudioMixer []	m_mixers;


	LibSequentiaAudioClipDepot	m_audioClipDepot;

	BeatSyncClock				m_clock;

	SectionPlayer []			m_secplayer		= new SectionPlayer[2];
	int							m_secPlayerIdx	= 0;

	Track						m_track;
	int							m_sectionIdx	= 0;

	void SwitchPlayer()
	{
		m_secPlayerIdx		= (m_secPlayerIdx + 1) % 2;
	}

	SectionPlayer currentPlayer
	{
		get { return m_secplayer[m_secPlayerIdx]; }
	}

	SectionPlayer sidePlayer
	{
		get { return m_secplayer[(m_secPlayerIdx+1)%2]; }
	}

	
	void Awake()
	{
		m_audioClipDepot	= gameObject.AddComponent<LibSequentiaAudioClipDepot>();

		m_secplayer[0]		= CreateSectionPlayer();
		m_secplayer[1]		= CreateSectionPlayer();

		var clipPack		= m_audioClipDepot.LoadAndMakeAudioPack("test-sec1-1", "test-sec1-2", "test-sec1-3", "test-sec1-4", "test-sec2-1", "test-sec2-2", "test-sec2-3", "test-sec2-4");
		m_track				= Track.GenTestTrack(120, clipPack);
		m_sectionIdx		= -1;
	}

	SectionPlayer CreateSectionPlayer()
	{
		var player1		= gameObject.AddComponent<LibSequentiaPlayer>();
		player1.SetTargetMixers(m_mixers);

		var player2		= gameObject.AddComponent<LibSequentiaPlayer>();
		player2.SetTargetMixers(m_mixers);

		var secPlayer	= new SectionPlayer(this);
		secPlayer.SetPlayerComponents(player1, player2);

		return secPlayer;
	}



	void Start()
	{
		m_clock	= new BeatSyncClock(m_track.BPM);
	}
	

	void Update()
	{
		bool doTransition		= false;
		double transitionTime	= 0;
		SectionPlayer.TransitionType ttype	= SectionPlayer.TransitionType.None;



		if (Input.GetKeyDown(KeyCode.N))			// 자연 진행
		{
			Debug.Log("Natural Transition");

			transitionTime	= m_clock.SecondPerBeat * 4;
			ttype			= SectionPlayer.TransitionType.Natural;
			doTransition	= true;
		}
		
		if (Input.GetKeyDown(KeyCode.M))			// 강제 진행
		{
			Debug.Log("Manual Transition");
			
			transitionTime	= m_clock.SecondPerBeat * 4;
			ttype			= SectionPlayer.TransitionType.Manual;
			doTransition	= true;
		}



		if (doTransition)
		{
			// 트랜지션을 해도 괜찮은 상황인지 체크

			if (sidePlayer.isReadyOrFinished)	// 다른쪽 플레이어가 재생중이 아니거나 루프 종료된 경우 스위칭. 새로 재생할 플레이어가 currentPlayer가 된다
			{
				SwitchPlayer();
				m_sectionIdx++;
				Debug.Log("switch player. section idx : " + m_sectionIdx);
			}


			// 여기서부터는 sidePlayer => 기존에 재생중이던 플레이어가 됨

			// 기존 재생중이던 플레이어가 트랜지션 진행중이지 않고, 좀더 상위의 트랜지션을 걸 때
			if (!sidePlayer.isOnTransition
				&& (int)ttype > (int)sidePlayer.currentEndTransition)
			{
				double newSectionStart;
				if(m_sectionIdx > 0)
				{
					newSectionStart	= sidePlayer.FadeoutSection(ttype, transitionTime);
				}
				else
				{
					newSectionStart	= m_clock.CalcNextSafeBeatTime();
				}


				if (m_sectionIdx < m_track.sectionCount)
				{
					currentPlayer.StartSection(m_track.GetSection(m_sectionIdx), m_clock, ttype, newSectionStart);
				}
			}
		}
	}
}
