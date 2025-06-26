<VirtualHost *:<<PORT>>> 
    DocumentRoot "<<PROJECT_DIR>>"
    ServerName <<HOSTNAME>>
    ServerAlias *.<<HOSTNAME>>
    <Directory "<<PROJECT_DIR>>">
        AllowOverride All
        Require all granted
    </Directory>
    ErrorLog "logs/<<SITENAME>>_error.log"
    CustomLog "logs/<<SITENAME>>_access.log" common
</VirtualHost>

# Auto-generated virtual host for <<SITENAME>>
