from django.urls import path
from . import views

urlpatterns = [
    path('', views.full, name='full'),
    path('full', views.full, name='full'),
    path('basic', views.basic, name='basic'),
]
