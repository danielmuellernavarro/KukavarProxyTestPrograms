from .models import Project
from django.urls import resolve


def projects(request):
    projects = Project.objects.all()
    return {'projects' : projects}

def nbar(request):
    current_url = request.get_full_path()
    # current_url = resolve(request.get_full_path).url_name
    # print(current_url)
    try:
        nbar = int(current_url.replace("/", ""))
    except:
        nbar = 0
    # print(nbar)
    return {'nbar' : nbar}
