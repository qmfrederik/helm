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

# Wait for the cluster to be ready
JSONPATH='{range .items[*]}{@.metadata.name}:{range @.status.conditions[*]}{@.type}={@.status};{end}{end}'; \
  until kubectl get nodes -o jsonpath="$JSONPATH" 2>&1 | grep -q "Ready=True"; do sleep 1; done

# Initialize helm, with RBAC permissions
kubectl create -f ~/ci/helm-rbac.yaml
helm init --service-account tiller