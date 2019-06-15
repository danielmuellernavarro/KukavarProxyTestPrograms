from django.shortcuts import render
from lib.py_openshowvar import openshowvar
from django.views.decorators.csrf import csrf_exempt
from .forms import StateForm, ReadForm, WriteForm
from django.http import JsonResponse
from django import forms
from .models import Variable, Project
from django.core import serializers
from django.http import HttpResponse
import json
import operator

KVP_ROBOT_PORT = 7000
KVP_DEBUG = False


def home(request):
    state_form = StateForm()
    project = Project.objects.all()[:1].get()
    state_form.fields["IP"].initial = project.IP
    read_form = ReadForm()
    write_form = WriteForm()
    return render(request, 'read_write_variable/home.html',
        {
        'state_form':state_form,
        'read_form':read_form,
        'write_form':write_form,
        })

def home_project(request, project=0):
    variables = Variable.objects.filter(project=project)
    project = Project.objects.get(pk=project)
    client = openshowvar(project.IP, KVP_ROBOT_PORT)

    jsondata_unsorted = [ob.as_json() for ob in variables]
    variables = sorted(jsondata_unsorted, key = lambda k:k['description'])
    # print(variables)
    # variables = sorted(jsondata_unsorted, key = lambda k:k['description'].lower())
    if client.can_connect:
        for item in variables:
            value = client.read(item['name'], KVP_DEBUG)
            value = response_openshowvar(value)
            item.update({'value':value})
    if request.is_ajax():
        return HttpResponse(json.dumps(variables), content_type='json')
    else:
        read_form = ReadForm()
        write_form = WriteForm()
        return render(request, 'read_write_variable/project/home.html',
            {
            'read_form':read_form,
            'write_form':write_form,
            'variables':variables,
            })

def refresh_table(request, project=0):
    variables = Variable.objects.filter(project=project)
    project = Project.objects.get(pk=project)
    client = openshowvar(project.IP, KVP_ROBOT_PORT)

    jsondata_unsorted = [ob.as_json() for ob in variables]
    variables = sorted(jsondata_unsorted, key = lambda k:k['description'])
    # print(variables)
    # variables = sorted(jsondata_unsorted, key = lambda k:k['description'].lower())
    if client.can_connect:
        for item in variables:
            value = client.read(item['name'], KVP_DEBUG)
            value = response_openshowvar(value)
            item.update({'value':value})
    if request.is_ajax():
        return HttpResponse(json.dumps(variables), content_type='json')
    else:
        read_form = ReadForm()
        write_form = WriteForm()
        return render(request, 'read_write_variable/project/home.html',
            {
            'state_form':state_form,
            'read_form':read_form,
            'write_form':write_form,
            'variables':variables,
            })

@csrf_exempt
def state(request):
    IP = request.POST.get('IP')
    f_ip = forms.GenericIPAddressField()
    try:
        f_ip.clean(IP)
        client = openshowvar(IP, KVP_ROBOT_PORT)
        if client.can_connect:
            robname = client.read('$ROBNAME[]', KVP_DEBUG)
            robname = response_openshowvar(robname)
            state = 'Connected to ' + robname
        else:
            state = 'Conection is not posible'
    except:
        state = 'Invalid IP'

    data = {
        'state': state,
    }
    return JsonResponse(data)

@csrf_exempt
def read_variable(request, project=0):
    if project == 0:
        IP = request.POST.get('IP')
    else:
        project = Project.objects.get(pk=project)
        IP = project.IP
    variable = request.POST.get('variable')
    client = openshowvar(IP, KVP_ROBOT_PORT)
    if client.can_connect:
        result = client.read(variable, KVP_DEBUG)
        result = response_openshowvar(result)
    else:
        result = 'Variable was not read'

    data = {
        'result': result
    }
    # print(request.POST)
    return JsonResponse(data)

@csrf_exempt
def write_variable(request, project=0):
    if project == 0:
        IP = request.POST.get('IP')
    else:
        project = Project.objects.get(pk=project)
        IP = project.IP
    variable = request.POST.get('variable')
    value = request.POST.get('value')
    client = openshowvar(IP, KVP_ROBOT_PORT)
    if client.can_connect:
        result = client.write(variable, value, KVP_DEBUG)
        result = response_openshowvar(result)
    else:
        result = 'Variable was not written'

    data = {
        'result': result,
    }
    return JsonResponse(data)

@csrf_exempt
def add(request, project=0):
    project = Project.objects.get(pk=project)
    IP = project.IP
    variable = request.POST.get('variable')
    new_variable = Variable()
    new_variable.name = variable
    new_variable.description = variable
    new_variable.project = project
    new_variable.save()

    data = {}
    return JsonResponse(data)

def response_openshowvar(value):
    print(value)
    if value == 'None':
        value = '<unknown value>'
    else:
        value = value[:-1]
        value = value[2:]
    return value
