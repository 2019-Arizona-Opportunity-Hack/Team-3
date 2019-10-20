﻿using CsvHelper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetCoreReact.Helpers;
using NetCoreReact.Models.Documents;
using NetCoreReact.Models.DTO;
using NetCoreReact.Services.Business.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NetCoreReact.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class CsvController : ControllerBase
	{
		private readonly IEventService _eventService;

		public CsvController(IEventService eventService)
		{
			this._eventService = eventService;
		}

		[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
		[HttpPost("[action]")]
		public async Task<DataResponse<string>> Upload(string eventID)
		{
			try
			{
				var emails = new List<string>();
				var files = Request.Form?.Files;
				int? fileCount = files?.Count;

				if (fileCount > 0)
				{
					for (int i = 0; i < fileCount; i++)
					{
						if (files[i].ContentType == null || files[i].Length == 0 || !files[i].ContentType.Equals("text/csv"))
						{
							return new DataResponse<string>()
							{
								Errors = new Dictionary<string, List<string>>()
								{
									["*"] = new List<string> { "File was empty or in the wrong format, please try again." },
								},
								Success = false
							};
						}

						using (var stream = new MemoryStream())
						{
							files[i].CopyTo(stream);
							stream.Seek(0, SeekOrigin.Begin);
							using (var reader = new StreamReader(stream))
							using (var csv = new CsvReader(reader))
							{
								emails.AddRange(csv.GetRecords<string>());
							}
						}
					}

					var currentEvent = await _eventService.GetEvent(eventID);
					var eventData = currentEvent.Data.FirstOrDefault();
					eventData.Participants.AddRange(emails.Select(x => new Participant() { Email = x, DateEntered = DateTime.UtcNow.ToString("o"), Type = ParticipantType.Attendee }));
					var updateEvent = _eventService.UpdateEvent(eventData);
					return new DataResponse<string>()
					{
						Success = true
					};
				}
				else
				{
					return new DataResponse<string>()
					{
						Errors = new Dictionary<string, List<string>>()
						{
							["*"] = new List<string> { "Request did not have any files attached, please try again." },
						},
						Success = false
					};
				}
			}
			catch (Exception ex)
			{
				LoggerHelper.Log(ex);
				return new DataResponse<string>()
				{
					Errors = new Dictionary<string, List<string>>()
					{
						["*"] = new List<string> { "Failed to upload csv, please try again." },
					},
					Success = false
				};
			}
		}

		[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
		[HttpGet("[action]")]
		public async Task<IActionResult> Download(string eventID)
		{
			try
			{
				var response = await _eventService.GetEvent(eventID);
				var emails = response?.Data?.FirstOrDefault()?.Participants?.Select(x => x.Email)?.ToList() ?? new List<string>();
				var result = Helpers.CsvHelper.WriteCsvToMemory(emails);
				var memoryStream = new MemoryStream(result);
				return new FileStreamResult(memoryStream, "text/csv") { FileDownloadName = $"{response.Data.FirstOrDefault().Title}-Email-Data.csv" };
			}
			catch (Exception ex)
			{
				LoggerHelper.Log(ex);
				return BadRequest(ex);
			}
		}
	}
}
