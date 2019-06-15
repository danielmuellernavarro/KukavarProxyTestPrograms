from django.shortcuts import render


def full(request):
    return render(request, 'metronic/demo1/home.html')

def basic(request):
    return render(request, 'metronic/basic/home.html')
