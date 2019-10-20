﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetCoreReact.Helpers;
using NetCoreReact.Models.Documents;
using NetCoreReact.Models.DTO;
using NetCoreReact.Services.Business;
using NetCoreReact.Services.Business.Interfaces;

namespace NetCoreReact.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ParticipantController : ControllerBase
	{
		private readonly IEventService _eventService;
		private readonly IEmailService _emailService;
		private readonly IAuthenticationService _authenticationService;

		public ParticipantController(IEventService eventService, IEmailService emailService, IAuthenticationService authenticationService)
		{
			this._eventService = eventService;
			this._emailService = emailService;
			this._authenticationService = authenticationService;
		}

		[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
		[HttpPost("[action]")]
		public async Task<DataResponse<Event>> AddParticipant([FromBody] DataParticipant newParticipant)
		{
			try
			{
				var response = await _eventService.AddParticipant(newParticipant);
				var currentEvent = await _eventService.GetEvent(newParticipant.EventId);
				var email = await _emailService.SendConfirmationEmail(newParticipant, currentEvent.Data.FirstOrDefault());
				return email;
			}
			catch (Exception ex)
			{
				LoggerHelper.Log(ex);
				return new DataResponse<Event>()
				{
					Errors = new Dictionary<string, List<string>>()
					{
						["*"] = new List<string> { "An exception occurred, please try again." },
					},
					Success = false
				};
			}
		}
	}
}
