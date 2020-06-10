# o365 login -t password -u $(username) -p $(password)

app=$(o365 teams app list -o json | jq '.[] | select(.externalId == "'"$APPLICATION_ID"'")')

if [ -z "$app" ]; then
  # install app
  o365 teams app publish -p "./package/second-demo-app.zip"
else
  # update app
  appId=$(echo $app | jq '.id')
  o365 teams app update -i $appId -p "./package/01-teams-app.zip"
fi
