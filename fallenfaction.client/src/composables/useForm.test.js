import { describe, it, expect, vi } from 'vitest'
import { useForm } from './useForm.js'

const flush = () => new Promise(resolve => setTimeout(resolve, 0))

describe('useForm', () => {
  it('initialises form, errors, and loading from initialValues', () => {
    const { form, errors, loading, message } = useForm({
      initialValues: { name: '', age: 0 },
      submit: async () => ({ success: true })
    })
    expect(form.name).toBe('')
    expect(form.age).toBe(0)
    expect(errors.name).toBe('')
    expect(errors.age).toBe('')
    expect(loading.value).toBe(false)
    expect(message.text).toBe('')
  })

  it('isValid follows the validator', () => {
    const { form, isValid } = useForm({
      initialValues: { name: '' },
      validate: f => (f.name ? {} : { name: 'required' }),
      submit: async () => ({ success: true })
    })
    expect(isValid.value).toBe(false)
    form.name = 'ok'
    expect(isValid.value).toBe(true)
  })

  it('blocks submit when validation fails and writes per-field errors', async () => {
    const submit = vi.fn(async () => ({ success: true }))
    const { errors, handleSubmit } = useForm({
      initialValues: { name: '' },
      validate: f => (f.name ? {} : { name: 'Name is required' }),
      submit
    })
    await handleSubmit()
    expect(errors.name).toBe('Name is required')
    expect(submit).not.toHaveBeenCalled()
  })

  it('toggles loading and calls onSuccess on a successful submit', async () => {
    const onSuccess = vi.fn()
    const { form, loading, message, handleSubmit } = useForm({
      initialValues: { name: '' },
      submit: async f => ({ success: true, message: `created ${f.name}` }),
      onSuccess
    })
    form.name = 'team'
    const promise = handleSubmit()
    expect(loading.value).toBe(true)
    await promise
    expect(loading.value).toBe(false)
    expect(message.type).toBe('success')
    expect(message.text).toBe('created team')
    expect(onSuccess).toHaveBeenCalledOnce()
  })

  it('maps PascalCase server validationErrors back onto camelCase client fields', async () => {
    const { errors, message, handleSubmit } = useForm({
      initialValues: { name: '', description: '' },
      submit: async () => ({
        success: false,
        error: 'Bad request',
        validationErrors: {
          Name: ['Name already taken'],
          Description: ['Too short', 'Trim it'],
          Unknown: ['ignored']
        }
      })
    })
    await handleSubmit()
    expect(errors.name).toBe('Name already taken')
    expect(errors.description).toBe('Too short')
    expect(message.type).toBe('error')
    expect(message.text).toBe('Bad request')
  })

  it('reports a generic error message when the submit promise rejects', async () => {
    const { message, loading, handleSubmit } = useForm({
      initialValues: { name: '' },
      submit: async () => { throw new Error('boom') }
    })
    await handleSubmit()
    expect(message.type).toBe('error')
    expect(message.text).toBe('An unexpected error occurred. Please try again.')
    expect(loading.value).toBe(false)
  })

  it('reset restores initial values and clears state', async () => {
    const { form, errors, message, reset, handleSubmit } = useForm({
      initialValues: { name: 'a', count: 1 },
      submit: async () => ({ success: false, error: 'nope', validationErrors: { Name: ['x'] } })
    })
    form.name = 'changed'
    form.count = 99
    await handleSubmit()
    await flush()
    expect(errors.name).toBe('x')
    reset()
    expect(form.name).toBe('a')
    expect(form.count).toBe(1)
    expect(errors.name).toBe('')
    expect(message.text).toBe('')
  })

  it('blocks a second handleSubmit while the first is in-flight', async () => {
    let resolveSubmit
    const submit = vi.fn(() => new Promise(resolve => { resolveSubmit = resolve }))
    const { handleSubmit } = useForm({
      initialValues: { name: 'x' },
      submit
    })

    const first = handleSubmit()
    const second = handleSubmit()
    expect(submit).toHaveBeenCalledTimes(1)

    resolveSubmit({ success: true })
    await first
    await second
    expect(submit).toHaveBeenCalledTimes(1)
  })

  it('clears prior errors at the start of each submit', async () => {
    let attempt = 0
    const { errors, handleSubmit } = useForm({
      initialValues: { name: 'x' },
      submit: async () => {
        attempt++
        return attempt === 1
          ? { success: false, validationErrors: { Name: ['first'] } }
          : { success: true }
      }
    })
    await handleSubmit()
    expect(errors.name).toBe('first')
    await handleSubmit()
    expect(errors.name).toBe('')
  })
})
