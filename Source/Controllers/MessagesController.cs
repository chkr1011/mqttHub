using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mqttHub.Mqtt;
using MQTTnet;
using MQTTnet.Protocol;

namespace mqttHub.Controllers;

[Authorize]
[ApiController]
public sealed class MessagesController : Controller
{
    readonly MqttServerService _mqttServerService;

    public MessagesController(MqttServerService mqttServerService)
    {
        _mqttServerService = mqttServerService ?? throw new ArgumentNullException(nameof(mqttServerService));
    }

    [Route("api/v1/messages")]
    [HttpPost]
    public ActionResult PostMessage(MqttApplicationMessage message)
    {
        _mqttServerService.Publish(message);
        return Ok();
    }

    [Route("api/v1/messages/{*topic}")]
    [HttpPost]
    public async Task<ActionResult> PostMessage(string topic, int qosLevel = 0)
    {
        byte[] payload;

        using (var memoryStream = new MemoryStream())
        {
            await HttpContext.Request.Body.CopyToAsync(memoryStream);
            payload = memoryStream.ToArray();
        }

        var message = new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(payload).WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qosLevel).Build();

        return PostMessage(message);
    }
}