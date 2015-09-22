using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace LibSequentia.Engine
{
	/// <summary>
	/// TrackPlayer 두 개를 운용하고, 둘 사이의 전환을 실행하는 플레이어
	/// </summary>
	public class MasterPlayer
	{
		// Constants

		const int				c_playerCount	= 2;


		/// <summary>
		/// MasterPlayer 내부에서 사용하는 메세지
		/// </summary>
		struct Message
		{
			public enum Type
			{
				NaturalProgress,		// 자연진행
				ManualProgress,			// 강제진행

				NewTrack,				// 새 트랙 올리기
			}

			public Type		type;
			public object	parameter;
		}



		// Members

		TrackPlayer []				m_trackPlayer		= new TrackPlayer[c_playerCount];				// track 플레이어
		Data.IAutomationControl []	m_trackTransCtrls	= new Data.IAutomationControl[c_playerCount];	// track 전환시 사용하는 오토메이션 컨트롤러

		int							m_playerIdx;														// 현재의 track 플레이어 인덱스

		TrackPlayer currentPlayer
		{
			get { return m_trackPlayer[m_playerIdx]; }
		}

		TrackPlayer sidePlayer
		{
			get { return m_trackPlayer[(m_playerIdx+1)%2]; }
		}

		void SwitchPlayer()
		{
			m_playerIdx	= (m_playerIdx + 1) % 2;
		}

		MonoBehaviour			m_context;
		Queue<Message>			m_msgQueue	= new Queue<Message>();		// 플레이어 내부 메세지 큐

		/// <summary>
		/// 가장 최근에 계산된 전환 시간.
		/// </summary>
		public double lastCalculatedTransitionTime { get; private set; }

		bool					m_newTrackReady;						// 새로 재생할 (트랜지션할) 트랙이 준비되어있는지 여부





		public MasterPlayer(MonoBehaviour context)
		{
			m_context	= context;
			context.StartCoroutine(UpdateCo());
		}

		public void DoNaturalProgress()
		{
			m_msgQueue.Enqueue(new Message() { type = Message.Type.NaturalProgress });
		}

		public void DoManualProgress()
		{
			m_msgQueue.Enqueue(new Message() { type = Message.Type.ManualProgress });
		}

		/// <summary>
		/// 새 트랙 올리기. 재생은 하지 않음. 다음번 진행 타이밍에 맞춰서 시작
		/// </summary>
		/// <param name="track"></param>
		public void SetNewTrack(Data.Track track)
		{
			m_msgQueue.Enqueue(new Message() { type = Message.Type.NewTrack, parameter = track });
		}

		private bool ProcessMessage(ref Message msg)
		{
			bool consume;
			switch(msg.type)
			{
				case Message.Type.ManualProgress:
					{
						var trtime	= currentPlayer.DoManualProgress();
						consume		= (trtime >= 0);

						if (consume)	// 정상적으로 처리된 경우에만 trtime을 트랜지션 시간으로 인정하여 보관한다.
							lastCalculatedTransitionTime = trtime;
					}
					break;

				case Message.Type.NaturalProgress:
					{
						var trtime	= currentPlayer.DoNaturalProgress();
						consume		= (trtime >= 0);

						if (consume)	// 정상적으로 처리된 경우에만 trtime을 트랜지션 시간으로 인정하여 보관한다.
							lastCalculatedTransitionTime = trtime;
					}
					break;

				case Message.Type.NewTrack:

					if (!sidePlayer.isPlaying)	// 반대쪽 플레이어가 현재 재생중이 아닌 경우만 세팅한다.
					{
						sidePlayer.SetTrack(msg.parameter as Data.Track, currentPlayer.clock);
						m_newTrackReady	= true;
						consume			= true;
					}
					else
					{
						consume = false;
					}
					break;

				default:
					Debug.LogError("unknown message : " + msg.type);
					consume	= true;
					break;
			}

			return consume;
		}

		IEnumerator UpdateCo()
		{
			while (true)
			{
				// 메세지 큐 처리
				while (m_msgQueue.Count > 0)
				{
					var msg	= m_msgQueue.Peek();

					if (ProcessMessage(ref msg))		// 메세지를 정상적으로 처리하면 큐에서 삭제
					{
						m_msgQueue.Dequeue();
					}
					else
					{									// 처리하지 못한 경우엔 큐에 존속시킨 채로 루프 빠져나오기
						break;
					}
				}



				yield return null;
			}
		}
	}
}