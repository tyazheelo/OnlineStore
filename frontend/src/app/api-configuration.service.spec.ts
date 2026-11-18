import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApiConfigurationService } from './api-configuration.service';
import { CartApiService } from './cart-api.service';

const apiConfiguration = {
  productApiUrl: 'https://products.example.test/api/Product',
  identityApiUrl: 'https://identity.example.test/api/identity',
  cartApiUrl: 'https://cart.example.test/api/Cart',
  orderApiUrl: 'https://orders.example.test/api/Order',
  supportApiUrl: 'https://orders.example.test/api/support',
};

describe('ApiConfigurationService', () => {
  let configuration: ApiConfigurationService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    configuration = TestBed.inject(ApiConfigurationService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the configured endpoints before exposing them', () => {
    configuration.load().subscribe();
    http.expectOne('/shop-config.json').flush(apiConfiguration);

    expect(configuration.productApiUrl).toBe(apiConfiguration.productApiUrl);
    expect(configuration.identityApiUrl).toBe(apiConfiguration.identityApiUrl);
    expect(configuration.cartApiUrl).toBe(apiConfiguration.cartApiUrl);
    expect(configuration.orderApiUrl).toBe(apiConfiguration.orderApiUrl);
    expect(configuration.supportApiUrl).toBe(apiConfiguration.supportApiUrl);
  });

  it('rejects invalid service URLs', () => {
    let error: unknown;
    configuration.load().subscribe({ error: (value: unknown) => (error = value) });
    http.expectOne('/shop-config.json').flush({
      ...apiConfiguration,
      productApiUrl: 'file:///local/products',
    });

    expect(error).toBeInstanceOf(Error);
    expect(() => configuration.productApiUrl).toThrowError(
      'API configuration has not been loaded.',
    );
  });
});

describe('CartApiService', () => {
  let api: CartApiService;
  let configuration: ApiConfigurationService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(CartApiService);
    configuration = TestBed.inject(ApiConfigurationService);
    http = TestBed.inject(HttpTestingController);
    configuration.load().subscribe();
    http.expectOne('/shop-config.json').flush(apiConfiguration);
  });

  afterEach(() => http.verify());

  it('sends authenticated cart updates to the configured cart API', () => {
    api.addItem('product-1', 2, 'test-token').subscribe();

    const request = http.expectOne(`${apiConfiguration.cartApiUrl}/items`);
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get('Authorization')).toBe('Bearer test-token');
    expect(request.request.body).toEqual({ productId: 'product-1', quantity: 2 });
    request.flush(null);
  });
});
