import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent {
  private _http: HttpClient;
  private _baseUrl: string;

  public vmConfig: VMConfig = new VMConfig();

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this._http = http;
    this._baseUrl = baseUrl;
  }

  onSubmit() {
    console.log(this.vmConfig);

    this._http.post(this._baseUrl + 'api/AzureMgmt/CreateVM', this.vmConfig).subscribe(
      (val) => {
        console.log("POST call successful value returned in body",
          val);
      },
      response => {
        console.log("POST call in error", response);
      },
      () => {
        console.log("The POST observable is now completed.");
      });
  }
}

class VMConfig {
  name: string;
  size: string;
}
