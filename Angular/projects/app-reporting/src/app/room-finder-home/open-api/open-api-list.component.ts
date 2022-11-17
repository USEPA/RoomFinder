import { Component, OnInit } from '@angular/core';
import { saveAs } from 'file-saver';
import { ReportingService } from '../../shared/services/reporting-service';
import { MsalService } from '@azure/msal-angular';
import { AuthResponse } from 'msal';

declare const SwaggerUIBundle: any;

@Component({
  selector: 'app-open-api-list',
  templateUrl: './open-api-list.component.html',
  styleUrls: ['./open-api-list.component.scss']
})
export class OpenApiListComponent implements OnInit {

  constructor(
    private msalService: MsalService,
    private metadataService: ReportingService) {
  }

  ngOnInit(): void {
    const swaggerUrl = this.metadataService.apiSpecificationUrl;
    const scopes = this.msalService.getScopesForEndpoint(swaggerUrl);

    this.msalService.acquireTokenSilent({ scopes })
      .then((response: AuthResponse) => {
        const accessToken = response.accessToken;
        const authHeader = `Bearer ${accessToken}`;


        SwaggerUIBundle({
          dom_id: '#swagger-ui',
          layout: 'BaseLayout',
          presets: [
            SwaggerUIBundle.presets.apis,
            SwaggerUIBundle.SwaggerUIStandalonePreset
          ],
          url: swaggerUrl,
          docExpansion: 'list',
          filter: true,
          operationsSorter: 'alpha',
          tagsSorter: 'alpha',
          requestInterceptor: (request: any) => {
            request.headers.Authorization = authHeader;
            return request;
          }
        });
      });
  }

  public downloadApiSpec() {
    this.metadataService.getApiSpecification().subscribe((file) => {
      console.log(`returned results ${new Date()}`);
      saveAs(file, 'api-spec.json');
    });
  }
}
