from django.contrib import admin
from django.urls import path, include
from django.conf import settings
from django.conf.urls.static import static
import metronic.views
import read_write_variable.views


urlpatterns = [
    path('', include('read_write_variable.urls')),
]

if settings.DEBUG:
    urlpatterns += static(settings.MEDIA_URL, document_root=settings.MEDIA_ROOT)
    import debug_toolbar
    urlpatterns = [
        path('__debug__/', include(debug_toolbar.urls)),
    ] + urlpatterns
    urlpatterns.append(path('admin/', admin.site.urls))
