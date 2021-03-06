﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Novus.Utilities;
using SimpleCore.Net;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;

// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib
{
	public sealed class SearchClient
	{
		public SearchClient(SearchConfig config)
		{
			Config = config;


			//Engines = GetAllEngines()
			//	.Where(e => Config.SearchEngines.HasFlag(e.Engine))
			//	.ToArray();

			//if (!Engines.Any()) {
			//	throw new SmartImageException("No engines specified");
			//}


			Results = new List<SearchResult>();
		}

		public SearchConfig Config { get; init; }

		public bool IsComplete { get; private set; }


		public BaseSearchEngine[] Engines => GetAllEngines()
			.Where(e => Config.SearchEngines.HasFlag(e.Engine))
			.ToArray();

		public List<SearchResult> Results { get; }


		public void Reset()
		{
			Results.Clear();
			IsComplete = false;
		}

		[CanBeNull]
		public ImageResult FindBestResult()
		{
			//todo: WIP

			var best = Results.Where(r => r.Status != ResultStatus.Extraneous && !r.IsPrimitive)
				.SelectMany(r =>
				{
					var x = r.OtherResults;
					x.Insert(0, r.PrimaryResult);
					return x;
				})
				.AsParallel()
				.Where(r => r.Url != null && ImageUtilities.IsDirectImage(r.Url.ToString()))
				.OrderByDescending(r => r.Similarity)
				.ThenByDescending(r => r.DetailScore)
				.FirstOrDefault();

			return best;
		}

		public async Task RefineSearchAsync()
		{
			if (!IsComplete) {
				throw new SmartImageException();
			}


			Trace.WriteLine($"Finding best result");

			var best = FindBestResult();

			if (best == null) {
				Trace.WriteLine($"Could not find best result");
				return;
			}

			var uri = best.Url;

			Trace.WriteLine($"Refining by {uri}");

			var img = uri;

			Config.Query = img;

			await RunSearchAsync();
		}

		public async Task<List<SearchResult>> MaximizeSearchAsync<T>(Func<SearchResult, T> property)
		{
			// TODO: WIP

			var t = RunSearchAsync();
			await t;

			var res = Results.OrderByDescending(property).ToList();

			res.RemoveAll(r => r.IsPrimitive);

			return res;
		}

		public async Task RunSearchAsync()
		{
			if (IsComplete) {
				Reset();
			}

			var tasks = new List<Task<SearchResult>>(Engines.Select(e => e.GetResultAsync(Config.Query)));

			while (!IsComplete) {
				var finished = await Task.WhenAny(tasks);

				var value = await finished;

				tasks.Remove(finished);

				Results.Add(value);

				// Call event
				ResultCompleted?.Invoke(this, new SearchResultEventArgs(value));

				IsComplete = !tasks.Any();
			}

			Trace.WriteLine($"[success] {nameof(SearchClient)}: Search complete");

		}

		public static BaseSearchEngine[] GetAllEngines()
		{


			return ReflectionHelper.GetAllImplementations<BaseSearchEngine>()
				.Select(Activator.CreateInstance)
				.Cast<BaseSearchEngine>()
				.ToArray();
		}

		#region Event

		public event EventHandler<SearchResultEventArgs> ResultCompleted;

		public class SearchResultEventArgs : EventArgs
		{
			public SearchResult Result { get; }

			public SearchResultEventArgs(SearchResult result)
			{

				Result = result;
			}
		}

		#endregion
	}
}