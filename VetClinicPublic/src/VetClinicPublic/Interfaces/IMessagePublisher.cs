﻿using System.Threading.Tasks;
using VetClinicPublic.Web.Models;

namespace VetClinicPublic.Web.Interfaces
{
  public interface IMessagePublisher
  {
    // for now we only need to publish one event type, so we're using its type specifically here.
    Task Publish(object eventToPublish);
  }
}
