using UniRx;
using UnityEngine;

public interface IQueryState
{
    BehaviorSubject<QueryState> IsLoading { get; }
    string ErrorMessage { get; }
}