#!/usr/bin/env bash
set -euo pipefail

mkdir -p certs
cd certs

# Generate CA if missing
if [ ! -f service-ca.crt ]; then
  openssl genrsa -out service-ca.key 2048
  openssl req -new -x509 -days 365 -key service-ca.key -out service-ca.crt \
    -subj "/CN=BANK-CA/OU=Service/O=OibBank/C=RS"
  echo "Generated service CA (service-ca.crt)"
fi

create_user_cert() {
  local name=$1
  local ou=$2
  local keyfile="${name}.key"
  local csrfile="${name}.csr"
  local crtfile="${name}.crt"

  openssl req -new -newkey rsa:2048 -nodes -keyout "$keyfile" -out "$csrfile" \
    -subj "/CN=${name}/OU=${ou}/O=OibBank/C=RS"
  openssl x509 -req -in "$csrfile" -CA service-ca.crt -CAkey service-ca.key -CAcreateserial -out "$crtfile" -days 365 -sha256
  rm -f "$csrfile"
  echo "Created $crtfile (CN=$name, OU=$ou)"
}

create_user_cert admin Sluzbenik
create_user_cert ivan Korisnik

echo "All certs created in certs/"
