using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using LibSequentia.Data;

namespace LibSequentia.Engine
{
	/// <summary>
	/// 트랙 재생을 관리
	/// </summary>
	public class TrackPlayer
	{
		// Constants

		const int					c_playerCount	= 2;


		// Members

		MonoBehaviour				m_context;
		BeatSyncClock				m_clock;

		SectionPlayer []			m_secplayer		= new SectionPlayer[c_playerCount];
		IAutomationControl []		m_secplayerCtrl	= new IAutomationControl[c_playerCount];
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



		public TrackPlayer(MonoBehaviour context)
		{
			m_context	= context;
		}

		/// <summary>
		/// SectionPlayer 추가
		/// </summary>
		/// <param name="player"></param>
		/// <param name="layermixers"></param>
		/// <param name="sectionMixerCtrl"></param>
		public void AttachSectionPlayer(SectionPlayer player, IAutomationControl sectionMixerCtrl)
		{
			// 적당한 idx 찾기
			int idx	= 0;
			for(; idx < c_playerCount; idx++)
			{
				if (m_secplayer[idx] == null)
					break;
			}

			if (idx >= c_playerCount)
				throw new System.InvalidOperationException();
			
			//
			m_secplayer[idx]		= player;
			m_secplayerCtrl[idx]	= sectionMixerCtrl;
		}

		/// <summary>
		/// Track을 새로 지정
		/// </summary>
		/// <param name="track"></param>
		/// <param name="clockToSync">기존에 존재하는 clock과 싱크를 맞춰야할 경우 지정</param>
		public void SetTrack(Track track, BeatSyncClock clockToSync = null)
		{
			m_track				= track;
			m_sectionIdx		= -1;

			if (clockToSync == null)								// 클럭이 지정되지 않은 경우, 새로 생성
			{
				m_clock			= new BeatSyncClock(track.BPM);
			}
			else if (clockToSync.BPM != track.BPM)					// 기존 클럭과 bpm이 다른 경우, 기존 클럭의 다음번 비트와 동기화하여 새로 클럭 생성
			{
				m_clock			= new BeatSyncClock(track.BPM, clockToSync.CalcNextSafeBeatTime());
			}
			else
			{														// 아니면 기존 클럭 그대로 사용
				m_clock			= clockToSync;
			}
		}

		/// <summary>
		/// 자연 진행
		/// </summary>
		/// <returns>transition이 발생할 시간. transition을 할 수 없었던 경우 -1 리턴</returns>
		public double DoNaturalProgress()
		{
			return DoProgress(SectionPlayer.TransitionType.Natural);
		}

		/// <summary>
		/// 강제 진행
		/// </summary>
		/// <returns>transition이 발생할 시간. transition을 할 수 없었던 경우 -1 리턴</returns>
		public double DoManualProgress()
		{
			return DoProgress(SectionPlayer.TransitionType.Manual);
		}

		double DoProgress(SectionPlayer.TransitionType ttype)
		{
			// 트랜지션을 해도 괜찮은 상황인지 체크

			if (sidePlayer.isReadyOrFinished)	// 다른쪽 플레이어가 재생중이 아니거나 루프 종료된 경우 스위칭. 새로 재생할 플레이어가 currentPlayer가 된다
			{
				SwitchPlayer();
				m_sectionIdx++;
				Debug.Log("switch player. section idx : " + m_sectionIdx);
			}


			// 트랜지션 시간 구하기

			int trbeats				= 4;
			if (m_sectionIdx < m_track.sectionCount)			// 다음에 올 섹션이 있다면 해당 섹션의 앞쪽 부분을 기준으로 계산
			{
				var nextsec			= m_track.GetSection(m_sectionIdx);
				trbeats				= nextsec.beatStart;

				if (ttype == SectionPlayer.TransitionType.Manual)	// 강제 전환은 fillin 앞부분이 잘림
				{
					trbeats			-= nextsec.beatFillIn;
				}
			}

			double transitionTime	= m_clock.CalcBeatTimeLength(trbeats);



			// 여기서부터는 sidePlayer => 기존에 재생중이던 플레이어가 됨

			double newSectionStart	= -1;

			// 기존 재생중이던 플레이어가 트랜지션 진행중이지 않고, 좀더 상위의 트랜지션을 걸 때
			if (!sidePlayer.isOnTransition
				&& (int)ttype > (int)sidePlayer.currentEndTransition)
			{
				
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

			return newSectionStart;
		}
	}
}