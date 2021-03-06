import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent {
  private _http: HttpClient;
  private _baseUrl: string;

  public vmList: VMConfig[];
  public vmConfig: VMConfig = new VMConfig();

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this._http = http;
    this._baseUrl = baseUrl;
    setInterval(() => {
      http.get<VMConfig[]>(baseUrl + 'api/AzureMgmt/ListVM').subscribe(result => {
        this.vmList = result;
        console.log(this.vmList)
      }, error => console.error(error));
    }, 5000);
  }

  onSubmit() {
    this._http.post(this._baseUrl + 'api/AzureMgmt/CreateVM', this.vmConfig).subscribe(
      (val) => {
        console.log("POST call successful value returned in body", val);
        alert("VM request submitted successfully.")
      },
      response => {
        console.log("POST call in error", response);
        alert("VM request submission failed.")
      },
      () => {
        console.log("The POST observable is now completed.");
      });
  }
}

class VMConfig {
  VMName: string;
  VMSize: string;
  ErrorMessage: string;
}
