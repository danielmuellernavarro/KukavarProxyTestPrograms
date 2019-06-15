from django.urls import path
from . import views

urlpatterns = [
    path('', views.home, name='home'),
    path('<int:project>/', views.home_project, name='home_project'),
    # path('<int:project_id>/', views.home_project, name='home_project'),
    path('ajax/state', views.state, name='state'),
    path('ajax/read_variable/<int:project>', views.read_variable, name='read_variable'),
    path('ajax/write_variable/<int:project>', views.write_variable, name='write_variable'),
    path('ajax/refresh_table/<int:project>', views.refresh_table, name='refresh_table'),
    path('ajax/add/<int:project>', views.add, name='add'),
]
