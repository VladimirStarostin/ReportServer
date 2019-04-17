﻿using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using ReportService.Interfaces.Core;

namespace ReportService.Nancy
{
    public class GeneralModule : NancyBaseModule
    {
        public GeneralModule()
        {
            ModulePath = "/api/v2";

            Get[""] = parameters =>
            {
                try
                {
                    var response = new Response {StatusCode = HttpStatusCode.OK};
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Get["/roles"] = parameters =>
            {
                try
                {
                    var claims = Context.CurrentUser.Claims.ToList();
                    ApiUserRole role = ApiUserRole.NoRole;

                    if (claims.Contains(EditPermission))
                        role = ApiUserRole.Editor;
                    else if (claims.Contains(StopRunPermission))
                        role = ApiUserRole.StopRunner;
                    else if (claims.Contains(ViewPermission))
                        role = ApiUserRole.Viewer;

                    var response = (Response) JsonConvert.SerializeObject(role);
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

        }
    }

    public class OpersModule : NancyBaseModule
    {
        public OpersModule(ILogic logic)
        {
            this.RequiresAnyClaim(ViewPermission,StopRunPermission, EditPermission);

            ModulePath = "/api/v2/opertemplates";

            Get[""] = parameters =>
            {
                try
                {
                    var response = (Response) logic.GetAllOperTemplatesJson();
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Get["/registeredimporters"] = parameters =>
            {
                try
                {
                    var response = (Response) logic.GetAllRegisteredImportersJson();
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Get["/registeredexporters"] = parameters =>
            {
                try
                {
                    var response = (Response) logic.GetAllRegisteredExportersJson();
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Get["/taskopers"] = parameters =>
            {
                try
                {
                    var response = (Response) logic.GetAllOperationsJson();
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Delete["/{id:int}"] = parameters =>
            {
                this.RequiresClaims(EditPermission);
                try
                {
                    logic.DeleteOperationTemplate(parameters.id);
                    return HttpStatusCode.OK;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Post[""] = parameters =>
            {
                this.RequiresClaims(EditPermission);
                try
                {
                    var newOper = this.Bind<DtoOperTemplate>();
                    var id = logic.CreateOperationTemplate(newOper);
                    var response = (Response) $"{id}";
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Put["/{id:int}"] = parameters =>
            {
                this.RequiresClaims(EditPermission);
                try
                {
                    var existingOper = this.Bind<DtoOperTemplate>
                        (new BindingConfig {BodyOnly = true});

                    if (parameters.id != existingOper.Id)
                        return HttpStatusCode.BadRequest;

                    logic.UpdateOperationTemplate(existingOper);
                    return HttpStatusCode.OK;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };
        }
    } //OperTemplates&Operations Module

    public class RecepientGroupsModule : NancyBaseModule
    {
        public RecepientGroupsModule(ILogic logic)
        {
            this.RequiresAnyClaim(ViewPermission, StopRunPermission, EditPermission);

            ModulePath = "/api/v2/recepientgroups";

            Get[""] = parameters =>
            {
                try
                {
                    var response = (Response) logic.GetAllRecepientGroupsJson();
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Delete["/{id:int}"] = parameters =>
            {
                this.RequiresClaims(EditPermission);
                try
                {
                    logic.DeleteRecepientGroup(parameters.id);
                    return HttpStatusCode.OK;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Post[""] = parameters =>
            {
                this.RequiresClaims(EditPermission);
                try
                {
                    var newReport = this.Bind<DtoRecepientGroup>();
                    var id = logic.CreateRecepientGroup(newReport);
                    var response = (Response) $"{id}";
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };


            Put["/{id:int}"] = parameters =>
            {
                this.RequiresClaims(EditPermission);
                try
                {
                    var existingGroup = this.Bind<DtoRecepientGroup>
                        (new BindingConfig {BodyOnly = true});

                    if (parameters.id != existingGroup.Id)
                        return HttpStatusCode.BadRequest;

                    logic.UpdateRecepientGroup(existingGroup);
                    return HttpStatusCode.OK;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };
        }
    } //RecepientGroupsModule

    public class TelegramModule : NancyBaseModule
    {
        public TelegramModule(ILogic logic)
        {
            this.RequiresAnyClaim(ViewPermission, StopRunPermission, EditPermission);

            ModulePath = "/api/v2/telegrams";

            Get[""] = parameters =>
            {
                try
                {
                    var response = (Response) logic.GetAllTelegramChannelsJson();
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Post[""] = parameters =>
            {
                this.RequiresClaims(EditPermission);
                try
                {
                    var newTelegramChannel = this.Bind<DtoTelegramChannel>();
                    var id = logic.CreateTelegramChannel(newTelegramChannel);
                    var response = (Response) $"{id}";
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Put["/{id:int}"] = parameters =>
            {
                this.RequiresClaims(EditPermission);
                try
                {
                    var existingTelegramChannel = this.Bind<DtoTelegramChannel>
                        (new BindingConfig {BodyOnly = true});

                    if (parameters.id != existingTelegramChannel.Id)
                        return HttpStatusCode.BadRequest;

                    logic.UpdateTelegramChannel(existingTelegramChannel);
                    return HttpStatusCode.OK;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

        }
    } //TelegramModule

    public class SyncModule : NancyBaseModule
    {
        public SyncModule(ILogic logic)
        {
            ModulePath = "/api/v2/synchronizations";

            Get["/currentexporters"] = parameters =>
            {
                try
                {
                    var response = (Response) logic.GetAllTelegramChannelsJson();
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Post[""] = parameters =>
            {
                this.RequiresClaims(EditPermission);
                try
                {
                    var apiTask = this.Bind<ApiTask>();
                    var id = logic.CreateTask(apiTask);
                    var response = (Response) $"{id}";
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };
        }
    } //TelegramModule

    public class ScheduleModule : NancyBaseModule
    {
        public ScheduleModule(ILogic logic)
        {
            this.RequiresAnyClaim(ViewPermission, StopRunPermission, EditPermission);

            ModulePath = "/api/v2/schedules";

            Get[""] = parameters =>
            {
                try
                {
                    var response = (Response) logic.GetAllSchedulesJson();
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Delete["/{id:int}"] = parameters =>
            {
                this.RequiresClaims(EditPermission);
                try
                {
                    logic.DeleteSchedule(parameters.id);
                    return HttpStatusCode.OK;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Post[""] = parameters =>
            {
                this.RequiresClaims(EditPermission);
                try
                {
                    var newSchedule = this.Bind<DtoSchedule>();
                    var id = logic.CreateSchedule(newSchedule);
                    var response = (Response) $"{id}";
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };


            Put["/{id:int}"] = parameters =>
            {
                this.RequiresClaims(EditPermission);
                try
                {
                    var existingSchedule = this.Bind<DtoSchedule>
                        (new BindingConfig {BodyOnly = true});

                    if (parameters.id != existingSchedule.Id)
                        return HttpStatusCode.BadRequest;

                    logic.UpdateSchedule(existingSchedule);
                    return HttpStatusCode.OK;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

        }
    } //SchedulesModule

    public class TasksModule : NancyBaseModule
    {
        public TasksModule(ILogic logic)
        {
            this.RequiresAnyClaim(ViewPermission, StopRunPermission, EditPermission);

            ModulePath = "/api/v2/tasks";

            Get[""] = parameters =>
            {
                try
                {
                    var response = (Response) logic.GetAllTasksJson();

                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Get["/stop/{taskinstanceid:long}", runAsync: true] = async (parameters, _) =>
            {
                this.RequiresAnyClaim(EditPermission, StopRunPermission);
                try
                {
                    var stopped = await logic.StopTaskByInstanceIdAsync(parameters.taskinstanceid);

                    var response = (Response) stopped.ToString();
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Get["/{taskid:int}/instances"] = parameters =>
            {
                try
                {
                    var response =
                        (Response) logic.GetAllTaskInstancesByTaskIdJson(parameters.taskid);
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Get["/{taskid:int}/currentviews", runAsync: true] = async (parameters, _) =>
            {
                try
                {
                    var response = (Response) await logic.GetCurrentViewByTaskIdAsync(parameters.taskid);
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Get["/run-{id:int}"] = parameters =>
            {
                this.RequiresAnyClaim(EditPermission, StopRunPermission);

                try
                {
                    string sentReps = logic.ForceExecute(parameters.id);
                    var response = (Response)sentReps;
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }

                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Get["/working-{taskId:int}"] = parameters =>
            {
                try
                {
                    string sentReps = logic.GetWorkingTasksByIdJson(parameters.taskId); 
                    var response = (Response)sentReps;
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }

                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Delete["/{id:int}"] = parameters =>
            {
                this.RequiresClaims(EditPermission);
                try
                {
                    logic.DeleteTask(parameters.id);
                    return HttpStatusCode.OK;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Post[""] = parameters =>
            {
                this.RequiresClaims(EditPermission);
                try
                {
                    var newTask = this.Bind<ApiTask>();
                    var id = logic.CreateTask(newTask);
                    var response = (Response) $"{id}";
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Put["/{id:int}"] = parameters =>
            {
                this.RequiresClaims(EditPermission);
                try
                {
                    var existingTask = this.Bind<ApiTask>
                        (new BindingConfig {BodyOnly = true});

                    if (parameters.id != existingTask.Id)
                        return HttpStatusCode.BadRequest;

                    logic.UpdateTask(existingTask);
                    return HttpStatusCode.OK;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };
        }
    } //TasksModule

    public class InstancesModule : NancyBaseModule
    {
        public InstancesModule(ILogic logic)
        {
            this.RequiresAnyClaim(ViewPermission, StopRunPermission, EditPermission);

            ModulePath = "/api/v2/instances";

            Delete["/{id:int}"] = parameters =>
            {
                this.RequiresClaims(EditPermission);
                try
                {
                    logic.DeleteTaskInstanceById(parameters.id);
                    return HttpStatusCode.OK;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            // TODO: filter - top, paginations
            Get[""] = parameters =>
            {
                try
                {
                    var response = (Response) logic.GetAllTaskInstancesJson();
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Get["/{instanceid:int}/operinstances"] = parameters =>
            {
                try
                {
                    var response = (Response) logic.GetOperInstancesByTaskInstanceIdJson(parameters.instanceid);
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };

            Get["/operinstances/{id:int}"] = parameters =>
            {
                try
                {
                    var response = (Response) logic.GetFullOperInstanceByIdJson(parameters.id);
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch
                {
                    return HttpStatusCode.InternalServerError;
                }
            };
        }
    } //Instances&OperInstancesModule
}
