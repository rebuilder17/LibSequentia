using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace LibSequentia.Data
{
	/// <summary>
	/// 오토메이션 정보를 읽고 실제 값을 조절하는 객체에 대한 인터페이스
	/// </summary>
	public interface IAutomationControl
	{
		void Set(float timeRatio, ICollection<Automation> data);
	}

	/// <summary>
	/// 파라미터 오토메이션에 관한 정보
	/// </summary>
	public class Automation
	{
		/// <summary>
		/// 오토메이션 타겟 목록
		/// </summary>
		public enum TargetParam
		{
			Volume,					// 볼륨
		}

		/// <summary>
		/// 오토메이션 컨트롤 포인트
		/// </summary>
		struct Point
		{
			public float timeRatio;		// 점이 위치하는 시간 비율
			public float value;			// 해당 점에서의 값
		}



		// Members

		List<Point>		m_data = new List<Point>();	// 이벤트 데이터, 시간순으로 정렬됨


		/// <summary>
		/// 오토메이션 타겟 파라미터
		/// </summary>
		public TargetParam targetParam { get; set; }



		/// <summary>
		/// 해당 시간 지점을 포함하는 두 인덱스 구하기
		/// </summary>
		/// <param name="timeRatio"></param>
		private void GetIndiciesForTimeRatio(float timeRatio, out int i1, out int i2)
		{
			i1		= 0;
			i2		= m_data.Count - 1;

			if (i2 < 0)									// 데이터가 없을 땐 (인덱스를 구할 수 없음) -1 대입 후 리턴
				return;

			while (i2 - i1 > 1)							// 간격이 1 이하로 좁아질 때 루프를 종료한다
			{
				var m	= (i2 + i1) / 2;

				if (m_data[m].timeRatio <= timeRatio)	// 현재 비교하는 중간 지점의 시간값이 현재 값보다 작거나 크다면, 이 점이 혹은 더 뒤쪽에 있는 지점이 찾는 지점일 것임.
				{
					i1 = m;
				}
				else
				{										// 아니라면 더 앞쪽에 찾는 지점이 있다는 것이므로 뒤쪽 범위를 앞으로 당겨온다
					i2 = m;
				}
			}
		}

		/// <summary>
		/// 조절점 추가
		/// </summary>
		/// <param name="timeRatio"></param>
		/// <param name="value"></param>
		public void AddPoint(float timeRatio, float value)
		{
			var newp		= new Point { timeRatio = timeRatio, value = value };
			int datacount	= m_data.Count;
			if (datacount == 0)							// 케이스 1 - 데이터가 없는 경우 바로 추가
			{
				m_data.Add(newp);
			}
			else
			{											// 데이터가 하나 이상일 경우엔 인덱스를 직접 구한다
				int i1, i2;
				GetIndiciesForTimeRatio(timeRatio, out i1, out i2);

				if (i1 == i2)							// 인덱스 간격이 아니라 정확히 한 인덱스를 가리키는 경우
				{
					if (i2 == (datacount - 1))			// 케이스 2 - 해당 인덱스가 마지막인 경우 add
					{
						m_data.Add(newp);
					}
					else
					{									// 케이스 3 - 해당 인덱스가 중간 혹은 처음에 오는 것일 경우, 인덱스 + 1 지점에 추가
						m_data.Insert(i2 + 1, newp);
					}
				}
				else
				{										// 그 외 모든 경우엔 인덱스 범위에서 뒤쪽 인덱스로 insert한다 (인덱스 범위 안쪽으로 추가됨)
					m_data.Insert(i2, newp);
				}
			}
		}

		/// <summary>
		/// 해당 시간 비율에서의 값을 구한다
		/// </summary>
		/// <param name="timeRatio"></param>
		/// <returns></returns>
		public float GetValue(float timeRatio)
		{
			int i1, i2;
			GetIndiciesForTimeRatio(timeRatio, out i1, out i2);

			if (i2 == -1)
				throw new System.InvalidOperationException("Automation data is empty");

			// 두 점 사이의 비율만큼 선형 보간해서 데이터 리턴
			var p1			= m_data[i1];
			var p2			= m_data[i2];
			var rbetween	= (timeRatio - p1.timeRatio) / (p2.timeRatio - p1.timeRatio);

			return p1.value + (p2.value - p1.value) * rbetween;
		}
	}
}
