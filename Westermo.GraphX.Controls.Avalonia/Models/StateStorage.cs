using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Westermo.GraphX.Common;
using Westermo.GraphX.Common.Exceptions;
using Westermo.GraphX.Common.Interfaces;
using Westermo.GraphX.Common.Models;
using QuikGraph;

namespace Westermo.GraphX.Controls.Avalonia.Models
{
    public class StateStorage<TVertex, TEdge, TGraph>(GraphArea<TVertex, TEdge, TGraph> area) : IDisposable
        where TEdge : class, IGraphXEdge<TVertex>
        where TVertex : class, IGraphXVertex
        where TGraph : class, IMutableBidirectionalGraph<TVertex, TEdge>
    {
        private readonly Dictionary<string, GraphState<TVertex, TEdge, TGraph>> _states = [];
        private GraphArea<TVertex, TEdge, TGraph> _area = area;

        /// <summary>
        /// Returns true if state with supplied ID exists in the current states collection
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool ContainsState(string id)
        {
            return _states.ContainsKey(id);
        }

        /// <summary>
        /// Save current graph state into memory (including visual and data controls)
        /// </summary>
        /// <param name="id">New unique state id</param>
        /// <param name="description">Optional state description</param>
        public virtual void SaveState(string id, string description = "")
        {
            _states.Add(id, GenerateGraphState(id, description));
        }

        /// <summary>
        /// Save current graph state into memory (including visual and data controls) or update existing state
        /// </summary>
        /// <param name="id">State id</param>
        /// <param name="description">Optional state description</param>
        public virtual void SaveOrUpdateState(string id, string description = "")
        {
            if (ContainsState(id))
                _states[id] = GenerateGraphState(id, description);
            else SaveState(id, description);
        }

        protected virtual GraphState<TVertex, TEdge, TGraph> GenerateGraphState(string id, string description = "")
        {
            if (_area.LogicCore == null)
                throw new GX_InvalidDataException("LogicCore -> Not initialized!");
            var vposlist = _area.VertexList.ToDictionary(item => item.Key, item => item.Value.GetPosition().ToGraphX());
            var vedgelist = _area.EdgesList.Where(item => item.Value.IsVisible).Select(item => item.Key).ToList();

            return new GraphState<TVertex, TEdge, TGraph>(id, _area.LogicCore.Graph, _area.LogicCore.AlgorithmStorage,
                vposlist, vedgelist, description);
        }

        /// <summary>
        /// Import specified state to the StateStorage
        /// </summary>
        /// <param name="key">State key</param>
        /// <param name="state">State object</param>
        public virtual void ImportState(string key, GraphState<TVertex, TEdge, TGraph> state)
        {
            if (ContainsState(key))
                throw new GX_ConsistencyException($"Graph state {key} already exist in state storage");
            _states.Add(key, state);
        }

        /// <summary>
        /// Load previously saved state into layout
        /// </summary>
        /// <param name="id">Unique state id</param>
        public virtual void LoadState(string id)
        {
            if (_area.LogicCore == null)
                throw new GX_InvalidDataException("GraphArea.LogicCore -> Not initialized!");

            if (!_states.TryGetValue(id, out GraphState<TVertex, TEdge, TGraph>? value))
            {
                Debug.WriteLine($"LoadState() -> State id {id} not found! Skipping...");
                return;
            }

            //One action: clear all, preload vertices, assign Graph property
            _area.PreloadVertexes(value.Graph, true, true);
            _area.LogicCore.Graph = value.Graph;
            _area.LogicCore.AlgorithmStorage = value.AlgorithmStorage;

            //setup vertex positions
            foreach (var item in value.VertexPositions)
            {
                _area.VertexList[item.Key].SetPosition(item.Value.X, item.Value.Y);
                _area.VertexList[item.Key].SetCurrentValue(GraphAreaBase.PositioningCompleteProperty, true);
            }

            //setup visible edges
            foreach (var item in value.VisibleEdges)
            {
                var edgectrl = _area.ControlFactory.CreateEdgeControl(_area.VertexList[item.Source],
                    _area.VertexList[item.Target],
                    item);
                _area.InsertEdge(item, edgectrl);
            }

            _area.UpdateLayout();
            foreach (var item in _area.EdgesList.Values)
            {
                item.UpdateEdge();
            }
        }

        /// <summary>
        /// Remove state by id
        /// </summary>
        /// <param name="id">Unique state id</param>
        public virtual void RemoveState(string id)
        {
            _states.Remove(id);
        }

        /// <summary>
        /// Get all states from the storage
        /// </summary>
        public virtual Dictionary<string, GraphState<TVertex, TEdge, TGraph>> GetStates()
        {
            return _states;
        }

        /// <summary>
        /// Get all states from the storage
        /// </summary>
        /// <param name="id">Unique state id</param>
        public virtual GraphState<TVertex, TEdge, TGraph>? GetState(string id)
        {
            return ContainsState(id) ? _states[id] : null;
        }

        public virtual void Dispose()
        {
            _states.ForEach(a => a.Value.Dispose());
            _states.Clear();
            _area = null!;
        }
    }
}