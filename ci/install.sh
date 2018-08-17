# Install .NET Core
sudo apt-get install -y curl apt-transport-https libc6-dev
curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-trusty-prod trusty main" | sudo tee /etc/apt/sources.list.d/dotnetdev.list > /dev/null
sudo apt-get update
sudo apt-get install -y dotnet-sdk-2.1.302

# Install PowerShell
curl https://packages.microsoft.com/config/ubuntu/14.04/prod.list | sudo tee /etc/apt/sources.list.d/microsoft.list > /dev/null
sudo apt-get update
sudo apt-get install -y powershell 

# Install kubectl
curl -Lo kubectl https://storage.googleapis.com/kubernetes-release/release/v1.9.0/bin/linux/amd64/kubectl
chmod +x kubectl
sudo mv kubectl /usr/local/bin/

# Install miniube
curl -Lo minikube https://storage.googleapis.com/minikube/releases/v0.25.0/minikube-linux-amd64
chmod +x minikube
sudo mv minikube /usr/local/bin/

# Install Helm
curl -sL https://kubernetes-helm.storage.googleapis.com/helm-v2.8.1-linux-amd64.tar.gz -o helm-v2.8.1-linux-amd64.tar.gz
mkdir ~/helm
tar xf helm-v2.8.1-linux-amd64.tar.gz -C ~/helm/
sudo mv ~/helm/linux-amd64/helm /usr/local/bin/helm
rm -rf ~/helm

# Create the minikube cluster
sudo minikube start --vm-driver=none --kubernetes-version=v1.9.0 --extra-config=apiserver.Authorization.Mode=RBAC
minikube update-context

# Disable the dashboard. We don't use it and it consumes a lot of resources
minikube addons disable dashboard

# Wait for the cluster nodes to be ready
JSONPATH='{range .items[*]}{@.metadata.name}:{range @.status.conditions[*]}{@.type}={@.status};{end}{end}'; \
  until kubectl get nodes -o jsonpath="$JSONPATH" 2>&1 | grep -q "Ready=True"; do sleep 1; done

# Initialize helm, with RBAC permissions
kubectl create -f $TRAVIS_BUILD_DIR/ci/helm-rbac.yaml
helm init --service-account tiller

# Wait for the Tiller pod to be online. helm init --wait has a timeout of 5 seconds, which is too
# optimistic for bootstrapping a cluster.

# Wait for the cluster to be ready
JSONPATH='{range .items[*]}{@.metadata.name}:{range @.status.conditions[*]}{@.type}={@.status};{end}{end}'; \
  until kubectl get pods -n kube-system -l app=helm,name=tiller -o jsonpath="$JSONPATH" 2>&1 | grep -q "Ready=True"; do kubectl get pods --all-namespaces; kubectl get pods -n kube-system -l app=helm,name=tiller; sleep 1; done
